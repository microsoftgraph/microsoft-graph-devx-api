using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace CodeSnippetsReflection.OpenAPI {
    public static class OpenApiSchemaExtensions {
        public static IEnumerable<KeyValuePair<string, OpenApiSchema>> GetAllProperties(this OpenApiSchema schema) {
            if(schema == null) return Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>();

            if (!schema.AllOf.Any() && !schema.AnyOf.Any() && !schema.OneOf.Any())
            {
                return schema.Properties
                        .Union(schema.AllOf.FlattenEmptyEntries(x => x.AllOf, 2).SelectMany(x => x.Properties))
                        .Union(schema.AnyOf.SelectMany(x => x.Properties))
                        .Union(schema.OneOf.SelectMany(x => x.Properties))
                        .Union(schema.Items != null ? schema.Items.AllOf.SelectMany(x => x.Properties) : Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>())
                        .Union(schema.Items != null ? schema.Items.AnyOf.SelectMany(x => x.AllOf.SelectMany(y => y.GetAllProperties()).Union(x.AnyOf.SelectMany(y => y.GetAllProperties()))) : Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>())
                        .Union(schema.Items != null ? schema.Items.AnyOf.SelectMany(x => x.Properties) : Enumerable.Empty<KeyValuePair<string, OpenApiSchema>>());
            }
            return schema.AllOf.Union(schema.AnyOf).Union(schema.OneOf).SelectMany(x => x.GetAllProperties());
        }
        public static OpenApiSchema GetPropertySchema(this OpenApiSchema schema, string propertyName) {
            if(string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if(schema == null) return null;
            return schema.GetAllProperties().FirstOrDefault(p => p.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).Value;
        }

        public static bool IsUntypedNode(this OpenApiSchema openApiSchema)
        {
            if (openApiSchema.Items != null)
                return openApiSchema.Items.IsUntypedNode();

            if (!openApiSchema.IsAllOf() && 
                !openApiSchema.IsOneOf() && 
                !openApiSchema.IsAnyOf() &&
                !openApiSchema.IsReferencedSchema() &&
                openApiSchema.Enum?.Count == 0 &&
                openApiSchema.Properties?.Count == 0 &&
                string.IsNullOrEmpty(openApiSchema.Type) &&
                string.IsNullOrEmpty(openApiSchema.Format) &&
                string.IsNullOrEmpty(openApiSchema.Title))
            {
                return true;
            }

            return false;
        }
    }
}
