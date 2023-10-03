using System.Collections.Generic;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph;

public record struct CodeProperty(string Name, string Value, List<CodeProperty> Children, PropertyType PropertyType = PropertyType.String, string TypeDefinition = null, string NamespaceName = null, bool isFlagsEnum = false);
