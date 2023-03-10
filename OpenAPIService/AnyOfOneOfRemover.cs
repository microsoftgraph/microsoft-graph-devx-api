// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Collections.Generic;
using System.Linq;

namespace OpenAPIService
{
    internal class AnyOfOneOfRemover : OpenApiVisitorBase
    {
        public override void Visit(OpenApiSchema schema)
        {
            if (schema.AnyOf?.Any() ?? false)
            {
                var newSchema = schema.AnyOf.FirstOrDefault();
                schema.AnyOf = null;
                FlattenSchema(schema, newSchema);
            }

            if (schema.OneOf?.Any() ?? false)
            {
                var newSchema = schema.OneOf.FirstOrDefault();
                schema.OneOf = null;
                FlattenSchema(schema, newSchema);
            }
        }

        private static void FlattenSchema(OpenApiSchema schema, OpenApiSchema newSchema)
        {
            if (newSchema != null)
            {
                if (newSchema.Reference != null)
                {
                    schema.Reference = newSchema.Reference;
                    schema.UnresolvedReference = true;
                }
                else
                {
                    // Copies schema properties based on https://github.com/microsoft/OpenAPI.NET.OData/pull/264.
                    CopySchema(schema, newSchema);
                }
            }
        }

        private static void CopySchema(OpenApiSchema schema, OpenApiSchema newSchema)
        {
            schema.Title ??= newSchema.Title;
            schema.Type ??= newSchema.Type;
            schema.Format ??= newSchema.Format;
            schema.Description ??= newSchema.Description;
            schema.Maximum ??= newSchema.Maximum;
            schema.ExclusiveMaximum ??= newSchema.ExclusiveMaximum;
            schema.Minimum ??= newSchema.Minimum;
            schema.ExclusiveMinimum ??= newSchema.ExclusiveMinimum;
            schema.MaxLength ??= newSchema.MaxLength;
            schema.MinLength ??= newSchema.MinLength;
            schema.Pattern ??= newSchema.Pattern;
            schema.MultipleOf ??= newSchema.MultipleOf;
            schema.Not ??= newSchema.Not;
            schema.Required ??= newSchema.Required;
            schema.Items ??= newSchema.Items;
            schema.MaxItems ??= newSchema.MaxItems;
            schema.MinItems ??= newSchema.MinItems;
            schema.UniqueItems ??= newSchema.UniqueItems;
            schema.Properties ??= newSchema.Properties;
            schema.MaxProperties ??= newSchema.MaxProperties;
            schema.MinProperties ??= newSchema.MinProperties;
            schema.Discriminator ??= newSchema.Discriminator;
            schema.ExternalDocs ??= newSchema.ExternalDocs;
            schema.Enum ??= newSchema.Enum;
            schema.ReadOnly = !schema.ReadOnly ? newSchema.ReadOnly : schema.ReadOnly;
            schema.WriteOnly = !schema.WriteOnly ? newSchema.WriteOnly : schema.WriteOnly;
            schema.Nullable = !schema.Nullable ? newSchema.Nullable : schema.Nullable;
            schema.Deprecated = !schema.Deprecated ? newSchema.Deprecated : schema.Deprecated;
        }
    }
}
