// THIS CLASS IS COPIED FROM KIOTA TO GET THE SAME NAMING CONVENTIONS, WE SHOULD FIND A WAY TO MUTUALIZE THE CODE
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace CodeSnippetsReflection.OpenAPI {
    public static class OpenApiOperationExtensions {
        private static readonly HashSet<string> successCodes = new() {"200", "201", "202"}; //204 excluded as it won't have a schema
        public static OpenApiSchema GetResponseSchema(this OpenApiOperation operation)
        {
            // Return Schema that represents all the possible success responses!
            // For the moment assume 200s and application/json
            var schemas = operation.Responses.Where(r => successCodes.Contains(r.Key))
                                .SelectMany(re => re.Value.Content)
                                .Where(c => c.Key == "application/json")
                                .Select(co => co.Value.Schema)
                                .Where(s => s is not null);

            return schemas.FirstOrDefault();
        }
    }

}
