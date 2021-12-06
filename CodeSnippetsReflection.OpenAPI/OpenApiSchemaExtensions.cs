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
                        .Union(schema.OneOf.SelectMany(x => x.Properties));
            }
            return schema.AllOf.Union(schema.AnyOf).Union(schema.OneOf).SelectMany(x => x.GetAllProperties());
        }
        public static OpenApiSchema GetPropertySchema(this OpenApiSchema schema, string propertyName) {
            if(string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));
            if(schema == null) return null;
            return schema.GetAllProperties().FirstOrDefault(p => p.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).Value;
        }
    }
}
