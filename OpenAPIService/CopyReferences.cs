using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Collections.Generic;

namespace OpenAPIService
{
    internal class CopyReferences : OpenApiVisitorBase
    {
        private readonly OpenApiDocument target;
        public OpenApiComponents Components = new OpenApiComponents();

        public CopyReferences(OpenApiDocument target)
        {
            this.target = target;
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

                case OpenApiExample example:
                    CopyComponentsExampleReference(example);
                    break;

                case OpenApiHeader header:
                    CopyComponentsHeaderReference(header);
                    break;

                case OpenApiSecurityScheme securityScheme:
                    CopyComponentsSecuritySchemeReference(securityScheme);
                    break;

                case OpenApiLink link:
                    CopyComponentsLinkReference(link);
                    break;

                case OpenApiCallback callBack:
                    CopyComponentsCallBackReference(callBack);
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
                EnsureSchemasExists();
                if (!Components.Schemas.ContainsKey(schema.Reference.Id))
                {
                    Components.Schemas.Add(schema.Reference.Id, schema);
                }
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

        private void CopyComponentsCallBackReference(OpenApiCallback callBack)
        {
            target.Components.Callbacks ??= new Dictionary<string, OpenApiCallback>();
            if (!Components.Callbacks.ContainsKey(callBack.Reference.Id))
            {
                Components.Callbacks.Add(callBack.Reference.Id, callBack);
            }
        }

        private void CopyComponentsLinkReference(OpenApiLink link)
        {
            target.Components.Links ??= new Dictionary<string, OpenApiLink>();
            if (!Components.Links.ContainsKey(link.Reference.Id))
            {
                Components.Links.Add(link.Reference.Id, link);
            }
        }

        private void CopyComponentsSecuritySchemeReference(OpenApiSecurityScheme securityScheme)
        {
            target.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
            if (!Components.SecuritySchemes.ContainsKey(securityScheme.Reference.Id))
            {
                Components.SecuritySchemes.Add(securityScheme.Reference.Id, securityScheme);
            }
        }

        private void CopyComponentsHeaderReference(OpenApiHeader header)
        {
            target.Components.Headers ??= new Dictionary<string, OpenApiHeader>();
            if (!Components.Headers.ContainsKey(header.Reference.Id))
            {
                Components.Headers.Add(header.Reference.Id, header);
            }
        }

        private void CopyComponentsExampleReference(OpenApiExample example)
        {
            target.Components.Examples ??= new Dictionary<string, OpenApiExample>();
            if (!Components.Examples.ContainsKey(example.Reference.Id))
            {
                Components.Examples.Add(example.Reference.Id, example);
            }
        }

        private void CopyComponentsRequestBodyReference(OpenApiRequestBody requestBody)
        {
            target.Components.RequestBodies ??= new Dictionary<string, OpenApiRequestBody>();
            if (!Components.RequestBodies.ContainsKey(requestBody.Reference.Id))
            {
                Components.RequestBodies.Add(requestBody.Reference.Id, requestBody);
            }
        }

        private void CopyComponentsResponseReference(OpenApiResponse response)
        {
            target.Components.Responses ??= new Dictionary<string, OpenApiResponse>();
            if (!Components.Responses.ContainsKey(response.Reference.Id))
            {
                Components.Responses.Add(response.Reference.Id, response);
            }
        }

        private void CopyComponentsParameterReference(OpenApiParameter parameter)
        {
            target.Components.Parameters ??= new Dictionary<string, OpenApiParameter>();
            if (!Components.Parameters.ContainsKey(parameter.Reference.Id))
            {
                Components.Parameters.Add(parameter.Reference.Id, parameter);
            }
        }

        private void CopyComponentsSchemaReference(OpenApiSchema schema)
        {
            EnsureSchemasExists();
            if (!Components.Schemas.ContainsKey(schema.Reference.Id))
            {
                Components.Schemas.Add(schema.Reference.Id, schema);
            }
        }
    }
}
