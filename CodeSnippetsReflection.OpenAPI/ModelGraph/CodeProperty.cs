using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph
{
    public record struct CodeProperty(string Name, string Value, List<CodeProperty> Children, PropertyType PropertyType = PropertyType.String);
}
