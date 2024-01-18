using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph;

public class ImportsGenerator{
    public List<Dictionary<string, string>> imports { get; set; } = new();
    public List<Dictionary<string, string>> GenerateImportTemplates(SnippetModel snippetModel){
        var codeGraph = new SnippetCodeGraph(snippetModel);
        if(codeGraph.Body.NamespaceName != null){
            var import = new Dictionary<string, string>();
            import.Add("Name", codeGraph.Body.Name);
            import.Add("TypeDefinition", codeGraph.Body.TypeDefinition);
            import.Add("NamespaceName", codeGraph.Body.NamespaceName);
            imports.Append(import);
        }
        if (codeGraph.Body.Children.Count > 0){
            foreach ( var child in codeGraph.Body.Children)
            {
                if(child.NamespaceName != null){
                    var childimport = new Dictionary<string, string>();
                    childimport.Add("Name", child.Name);
                    childimport.Add("TypeDefinition", child.TypeDefinition);
                    childimport.Add("NamespaceName", child.NamespaceName);
                    imports.Append(childimport);
                }
                if (child.Children.Count > 0){
                    var grandchildimport = new Dictionary<string, string>();
                    foreach ( var grandchild in child.Children)
                    {
                        if(grandchild.NamespaceName != null){
                            grandchildimport.Add("Name", grandchild.Name);
                            grandchildimport.Add("TypeDefinition", grandchild.TypeDefinition);
                            grandchildimport.Add("NamespaceName", grandchild.NamespaceName);
                            imports.Append(grandchildimport);
                        }
                        if(grandchild.Children != null){
                            var grandchild2import = new Dictionary<string, string>();
                            foreach ( var grandchild2 in grandchild.Children)
                            {
                                if(grandchild2.NamespaceName != null){
                                    grandchildimport.Add("Name", grandchild2.Name);
                                    grandchildimport.Add("TypeDefinition", grandchild2.TypeDefinition);
                                    grandchildimport.Add("NamespaceName", grandchild2.NamespaceName);
                                    imports.Append(grandchildimport);
                                }
                            }
                        }
                    }
                }
            }
        
        }
        return imports;
    }
    


}
