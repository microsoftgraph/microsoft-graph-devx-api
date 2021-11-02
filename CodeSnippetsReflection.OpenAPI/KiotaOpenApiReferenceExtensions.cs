// THIS CLASS IS COPIED FROM KIOTA TO GET THE SAME NAMING CONVENTIONS, WE SHOULD FIND A WAY TO MUTUALIZE THE CODE
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;

namespace CodeSnippetsReflection.OpenAPI {
    public static class OpenApiReferenceExtensions {
        public static string GetClassName(this OpenApiReference reference) {
            var referenceId = reference?.Id;
            return referenceId?[((referenceId?.LastIndexOf('.') ?? 0) + 1)..]
                                          ?.ToFirstCharacterUpperCase();
        }
    }
}
