// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Text;

namespace OpenAPIService
{
   /// <summary>
   /// Apply changes to ensure compatibility with the AutoREST PowerShell generator
   /// </summary>
    internal class PowershellFormatter : OpenApiVisitorBase
    {
        private const string DefaultPutPrefix = ".Update";
        private const string NewPutPrefix = "_Set";

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
                    StringBuilder newOperationId = new StringBuilder(operationId);

                    newOperationId.Replace(DefaultPutPrefix, NewPutPrefix);
                    pathItem.Operations[putOperation].OperationId = newOperationId.ToString();
                }
            }
        }

        /// <summary>
        /// The last '.' character of the OperationId value separates the method group from the operation name.
        /// This is replaced with an '_' to format the OperationId to allow for the creation of logical Powershell cmdlet names
        /// </summary>
        public override void Visit(OpenApiOperation operation)
        {
            var operationId = operation.OperationId;

            int charPos = operationId.LastIndexOf('.', operationId.Length - 1);

            // Check whether Put operation id already got updated
            if (charPos >= 0 && !operationId.Contains(NewPutPrefix))
            {
                StringBuilder newOperationId = new StringBuilder(operationId);

                newOperationId[charPos] = '_';
                operation.OperationId = newOperationId.ToString();
            }
        }

        public override void Visit(OpenApiSchema schema)
        {
            if (schema?.Type == "object")
            {
                schema.AdditionalProperties = new OpenApiSchema() {Type = "object"};  // To make AutoREST happy
            }
            base.Visit(schema);
        }
    }
}
