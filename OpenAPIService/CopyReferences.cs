﻿using Microsoft.OpenApi.Interfaces;
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
            switch (referenceable)
            {
                case OpenApiSchema schema:
                    EnsureComponentsExists();
                    EnsureSchemasExists();
                    if (!Components.Schemas.ContainsKey(schema.Reference.Id))
                    {
                        Components.Schemas.Add(schema.Reference.Id, schema);
                    }
                    break;

                case OpenApiParameter parameter:
                    EnsureComponentsExists();
                    EnsureParametersExists();
                    if (!Components.Parameters.ContainsKey(parameter.Reference.Id))
                    {
                        Components.Parameters.Add(parameter.Reference.Id, parameter);
                    }
                    break;

                case OpenApiResponse response:
                    EnsureComponentsExists();
                    EnsureResponsesExists();
                    if (!Components.Responses.ContainsKey(response.Reference.Id))
                    {
                        Components.Responses.Add(response.Reference.Id, response);
                    }
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
            if (target.Components == null)
            {
                target.Components = new OpenApiComponents();
            }
        }

        private void EnsureSchemasExists()
        {
            if (target.Components.Schemas == null)
            {
                target.Components.Schemas = new Dictionary<string, OpenApiSchema>();
            }
        }

        private void EnsureParametersExists()
        {
            if (target.Components.Parameters == null)
            {
                target.Components.Parameters = new Dictionary<string, OpenApiParameter>();
            }
        }

        private void EnsureResponsesExists()
        {
            if (target.Components.Responses == null)
            {
                target.Components.Responses = new Dictionary<string, OpenApiResponse>();
            }
        }
    }
}
