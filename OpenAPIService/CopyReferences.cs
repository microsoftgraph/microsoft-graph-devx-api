using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Collections.Generic;

namespace OpenAPIService
{
    internal class CopyReferences : OpenApiVisitorBase
    {
        private readonly OpenApiDocument target;
        private readonly OpenApiComponents allComponents;
        public OpenApiComponents Components = new();

        public CopyReferences(OpenApiDocument target, OpenApiComponents allComponents)
        {
            this.target = target;
            this.allComponents = allComponents;
        }

        public override void Visit(IOpenApiReferenceable referenceable)
        {
            EnsureComponentsExists();
            switch (referenceable)
            {
                case OpenApiSchema schema:
                    CopyComponentsSchemaReference(schema);
                    break;

                case OpenApiParameter parameter:
                    CopyComponentsParameterReference(parameter);
                    break;

                case OpenApiResponse response:
                    CopyComponentsResponseReference(response);
                    break;

                case OpenApiRequestBody requestBody:
                    CopyComponentsRequestBodyReference(requestBody);
                    break;

                default:
                    break;
            }
            base.Visit(referenceable);
        }

        public override void Visit(OpenApiSchema schema)
        {
            // This is needed to handle schemas used in Responses in components
            if (schema.Reference != null)
            {
                EnsureComponentsExists();
                CopyComponentsSchemaReference(schema);
            }
            base.Visit(schema);
        }

        private void EnsureComponentsExists()
        {
            target.Components ??= new OpenApiComponents();
        }

        private void EnsureSchemasExists()
        {
            target.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
        }

        private void CopyComponentsRequestBodyReference(OpenApiRequestBody requestBody)
        {
            target.Components.RequestBodies ??= new Dictionary<string, OpenApiRequestBody>();
            if (!Components.RequestBodies.ContainsKey(requestBody.Reference.Id))
            {
                Components.RequestBodies.Add(
                    requestBody.Reference.Id,
                    allComponents.RequestBodies[requestBody.Reference.Id]);
            }
        }

        private void CopyComponentsResponseReference(OpenApiResponse response)
        {
            target.Components.Responses ??= new Dictionary<string, OpenApiResponse>();
            if (!Components.Responses.ContainsKey(response.Reference.Id))
            {
                Components.Responses.Add(
                    response.Reference.Id,
                    allComponents.Responses[response.Reference.Id]);
            }
        }

        private void CopyComponentsParameterReference(OpenApiParameter parameter)
        {
            target.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
            if (!Components.Parameters.ContainsKey(parameter.Reference.Id))
            {
                Components.Parameters.Add(
                    parameter.Reference.Id,
                    allComponents.Parameters[parameter.Reference.Id]);
            }
        }

        private void CopyComponentsSchemaReference(OpenApiSchema schema)
        {
            EnsureSchemasExists();
            if (!Components.Schemas.ContainsKey(schema.Reference.Id))
            {
                Components.Schemas.Add(
                    schema.Reference.Id,
                    allComponents.Schemas[schema.Reference.Id]);
            }
        }
    }
}
