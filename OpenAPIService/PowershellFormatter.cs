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

                newOperationId[charPos] = '_';
                operation.OperationId = newOperationId.ToString();
            }
        }

        public override void Visit(OpenApiSchema schema)
        {
            schema.AdditionalPropertiesAllowed = true;
            base.Visit(schema);
        }
    }
}
