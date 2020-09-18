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
        private const string DefaultPatchPrefix = ".Update";
        private const string NewPatchPrefix = "_Set";

        /// <summary>
        /// The last '.' character of the OperationId value separates the method group from the operation name.
        /// This is replaced with an '_' to format the OperationId to allow for the creation of logical Powershell cmdlet names
        /// </summary>
        public override void Visit(OpenApiOperation operation)
        {
            var operationId = operation.OperationId;

            int charPos = operationId.LastIndexOf('.', operationId.Length - 1);
            if (charPos >= 0)
            {
                StringBuilder newOperationId = new StringBuilder(operationId);

                // Patch operation id should have the format -> {xxx}_Set{Yyy}
                if (operationId.Contains(DefaultPatchPrefix))
                {
                    newOperationId.Replace(DefaultPatchPrefix, NewPatchPrefix);
                }
                else
                {
                    newOperationId[charPos] = '_';
                }

                operation.OperationId = newOperationId.ToString();
            }
        }

        public override void Visit(OpenApiSchema schema)
        {
            if (schema?.Type == "object") {
                schema.AdditionalProperties = new OpenApiSchema() {Type = "object"};  // To make AutoREST happy
            }
            base.Visit(schema);
        }
    }
}
