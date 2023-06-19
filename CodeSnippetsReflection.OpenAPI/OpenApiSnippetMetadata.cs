using System.Collections.Generic;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI;

public record OpenApiSnippetMetadata(OpenApiUrlTreeNode OpenApiUrlTreeNode, IDictionary<string, OpenApiSchema> Schemas);

