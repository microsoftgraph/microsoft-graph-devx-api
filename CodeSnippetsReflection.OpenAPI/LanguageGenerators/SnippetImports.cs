using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph;

public class ImportsGenerator{
    public List<Dictionary<string, object>> imports { get; set; } = new();
    public List<Dictionary<string, object>> GenerateImportTemplates(SnippetModel snippetModel){
        var codeGraph = new SnippetCodeGraph(snippetModel);
        foreach ( var child in codeGraph.Body.Children){
            var import = new Dictionary<string, object>();
            import.Add("Name", child.Name);
            import.Add("TypeDefinition", child.TypeDefinition);
            import.Add("NameSpace", child.NamespaceName);
            imports.Append(import);
            
        }
        return imports;
    }
    


}
