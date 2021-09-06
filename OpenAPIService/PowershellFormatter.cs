// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            }

            var charPos = operationId.LastIndexOf('.', operationId.Length - 1);

            // Check whether Put operation id already got updated
            if (charPos >= 0 && !operationId.Contains(NewPutPrefix))
            {
                var newOperationId = new StringBuilder(operationId);

                newOperationId[charPos] = '_';
                operationId = newOperationId.ToString();
            }

            // Change Ref operationId name
            // Ref key word is enclosed between lower-cased and upper-cased letters
            // Ex.: applications_GetRefCreatedOnBehalfOf to applications_GetCreatedOnBehalfOfByRef
            var regex = new Regex("(?<=[a-z])Ref(?=[A-Z])");
            if (regex.Match(operationId).Success)
            {
                operationId = $"{regex.Replace(operationId, string.Empty)}ByRef";
            }

            operation.OperationId = operationId;
        }

        /// <summary>
        /// Visits an <see cref="OpenApiSchema"/>
        /// </summary>
        /// <param name="schema">The target <see cref="OpenApiSchema"/></param>
        public override void Visit(OpenApiSchema schema)
        {
            if (_schemaLoop.Contains(schema))
            {
                return; // loop detected, this schema has already been walked.
            }

            if ("object".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase))
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
        /// Resolves action and function OperationIds by reverting their signatures
        /// to how they were being defined in package Microsoft.OpenApi.OData ver. 1.0.6 and below.
        /// </summary>
        /// <remarks>
        /// This is to prevent change of cmdlet names already defined using the previous version's format.
        /// </remarks>
        /// <example>
        /// Default OperationId --> communications.calls.call_keepAlive
        /// Resolved OperationId --> communications.calls_keepAlive
        /// </example>
        /// <param name="operation">The target OpenAPI operation.</param>
        /// <returns>The resolved OperationId.</returns>
        private static string ResolveActionFunctionOperationId(OpenApiOperation operation)
        {
            var operationId = operation.OperationId;
            var segments = operationId.Split(new char[] {'.'}, StringSplitOptions.RemoveEmptyEntries).ToList();

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

            return string.Join(".", segments);
        }
    }
}
