using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using System.Linq;

namespace OpenAPIService
{
    internal class AnyOfRemover : OpenApiVisitorBase
    {
        public override void Visit(OpenApiSchema schema)
        {
            if (schema.AnyOf != null && schema.AnyOf.Count > 0)
            {
                var newSchema = schema.AnyOf.FirstOrDefault();
                schema.AnyOf = null;
                if (newSchema != null)
                {
                    if (newSchema.Reference != null)
                    {
                        schema.Reference = newSchema.Reference;
                        schema.UnresolvedReference = true;
                    }
                    else
                    {
                        schema.Type = newSchema.Type;
                    }
                }
            }
        }
    }
}
