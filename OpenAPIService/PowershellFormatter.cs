// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Humanizer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace OpenAPIService
{
    /// <summary>
    /// Apply changes to ensure compatibility with the AutoREST PowerShell generator
    /// </summary>
    internal class PowershellFormatter : OpenApiVisitorBase
    {
        private const string DefaultPutPrefix = ".Update";
        private const string NewPutPrefix = "_Set";
        private readonly Stack<OpenApiSchema> _schemaLoop = new();

        /// <summary>
        /// Accesses the individual OpenAPI operations for a particular OpenApiPathItem.
        /// </summary>
        /// <param name="pathItem">The OpenApiPathItem.</param>
        public override void Visit(OpenApiPathItem pathItem)
        {
            var putOperation = OperationType.Put;

            // For PowerShell, Put operation id should have the format -> {xxx}_Set{Yyy}
            if (pathItem.Operations.ContainsKey(putOperation))
            {
                var operationId = pathItem.Operations[putOperation].OperationId;
                operationId = SingularizeAndDeduplicateOperationId(operationId);

                if (operationId.Contains(DefaultPutPrefix))
                {
                    var newOperationId = new StringBuilder(operationId);

                    newOperationId.Replace(DefaultPutPrefix, NewPutPrefix);
                    pathItem.Operations[putOperation].OperationId = newOperationId.ToString();
                }
            }
        }

        /// <summary>
        /// Visits an <see cref="OpenApiOperation"/>
        /// </summary>
        /// <remarks>
        /// The last '.' character of the OperationId value separates the method group from the operation name.
        /// This is replaced with an '_' to format the OperationId to allow for the creation of logical Powershell cmdlet names.
        /// </remarks>
        /// <param name="operation">The target <see cref="OpenApiOperation"/></param>
        public override void Visit(OpenApiOperation operation)
        {
            var operationId = operation.OperationId;

            if (operation.Extensions.TryGetValue("x-ms-docs-operation-type",
                                                  out var value) && value != null)
            {
                var operationType = (value as OpenApiString)?.Value;

                if ("action".Equals(operationType, StringComparison.OrdinalIgnoreCase) ||
                    "function".Equals(operationType, StringComparison.OrdinalIgnoreCase))
                {
                    // Only valid if Microsoft.OpenApi.OData package ver. 1.0.7 and greater is used.
                    // This fix: https://github.com/microsoft/OpenAPI.NET.OData/pull/98
                    // in the above library changed how action and function OperationIds are constructed.
                    // To maintain pre-existing cmdlet names in PowerShell, we need to resolve their
                    // OperationIds to how they were being constructed in earlier versions of the lib.
                    operationId = ResolveActionFunctionOperationId(operation);
                }

                if ("function".Equals(operationType, StringComparison.OrdinalIgnoreCase))
                {
                    ResolveFunctionParameters(operation);
                }
            }

            operationId = SingularizeAndDeduplicateOperationId(operationId);

            var charPos = operationId.LastIndexOf('.', operationId.Length - 1);

            // Check whether Put operation id already got updated
            if (charPos >= 0 && !operationId.Contains(NewPutPrefix))
            {
                var newOperationId = new StringBuilder(operationId);

                newOperationId[charPos] = '_';
                operationId = newOperationId.ToString();
            }

            // Update $ref path operationId name
            // Ref key word is enclosed between lower-cased and upper-cased letters
            // Ex.: applications_GetRefCreatedOnBehalfOf to applications_GetCreatedOnBehalfOfByRef
            var regex = new Regex("(?<=[a-z])Ref(?=[A-Z])", RegexOptions.None, TimeSpan.FromSeconds(5));
            if (regex.Match(operationId).Success)
            {
                operationId = $"{regex.Replace(operationId, string.Empty)}ByRef";
            }

            operation.OperationId = operationId;
            FormatODataTypeCastSegmentOperationId(operation);
        }

        /// <summary>
        /// Visits an <see cref="OpenApiSchema"/>
        /// </summary>
        /// <param name="schema">The target <see cref="OpenApiSchema"/></param>
        public override void Visit(OpenApiSchema schema)
        {
            if (schema != null && !_schemaLoop.Contains(schema) && "object".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase))
            {
                schema.AdditionalProperties = new OpenApiSchema() { Type = "object" }; // To make AutoREST happy

                /* Because 'additionalProperties' are now being walked,
                 * we need a way to keep track of visited schemas to avoid
                 * endlessly creating and walking them in an infinite recursion.
                 */
                _schemaLoop.Push(schema.AdditionalProperties);
            }
        }

        /// <summary>
        /// Resolves the OperationIds of action and function paths.
        /// </summary>
        /// <param name="operation">The target OpenAPI operation.</param>
        /// <returns>The resolved OperationId.</returns>
        private static string ResolveActionFunctionOperationId(OpenApiOperation operation)
        {
            var operationId = operation.OperationId;
            var segments = operationId.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Remove ODataKeySegment values from OperationIds of actions and functions paths.
            // This is to prevent breaking changes of OperationId values already
            // defined using package Microsoft.OpenApi.OData ver. 1.0.6 and below.
            // For example,
            // Default OperationId --> communications.calls.call_keepAlive
            // Resolved OperationId --> communications.calls_keepAlive
            foreach (var parameter in operation.Parameters)
            {
                // Get the ODataKeySegment value.
                // Paths containing the above segment are
                // the ones needing to be resolved.
                if (parameter.Extensions.TryGetValue("x-ms-docs-key-type",
                                                     out var value) && value != null)
                {
                    var keyType = (value as OpenApiString)?.Value;

                    if (!string.IsNullOrEmpty(keyType) && operationId.Contains(keyType))
                    {
                        segments.Remove(keyType);
                    }
                }
            }

            var updatedOperationId = string.Join(".", segments);

            // Remove hash suffix values from OperationIds of function paths.
            // For example,
            // Default OperationId --> reports_getEmailActivityUserDetail-fe32
            // Resolved OperationId --> reports_getEmailActivityUserDetail
            var regex = new Regex(@"^[^-]+", RegexOptions.None, TimeSpan.FromSeconds(5));
            updatedOperationId = regex.Match(updatedOperationId).Value;

            return updatedOperationId;
        }

        /// <summary>
        /// Resolves structured or collection-valued function parameters.
        /// </summary>
        /// <param name="operation">The target OpenAPI operation of the function.</param>
        private static void ResolveFunctionParameters(OpenApiOperation operation)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (parameter.Content?.Any() ?? false)
                {
                    // Replace content with a schema object of type array
                    // for structured or collection-valued function parameters
                    parameter.Content = null;
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string"
                        }
                    };
                }
            }
        }

        private static void FormatODataTypeCastSegmentOperationId(OpenApiOperation operation)
        {
            // Replace fully qualified OData cast segments' namespace name with 'As'
            var graphNamespace = "microsoft.graph.";
            while (operation.OperationId.Contains(graphNamespace))
            {
                var namespaceIndex = operation.OperationId.IndexOf(graphNamespace);
                var part1 = operation.OperationId.Substring(0, namespaceIndex);
                var targetCharIndex = namespaceIndex + graphNamespace.Length;
                var targetChar = operation.OperationId[targetCharIndex];
                var part2 = operation.OperationId.Substring(targetCharIndex + 1);

                targetChar = char.ToUpperInvariant(targetChar);

                operation.OperationId = part1 + "As" + targetChar + part2;
            }
        }
    }
}
