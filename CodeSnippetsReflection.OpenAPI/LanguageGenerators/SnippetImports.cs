using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph;

public class ImportsGenerator{
    public List<Dictionary<string, string>> imports  = new();
    public List<Dictionary<string, string>> GenerateImportTemplates(SnippetModel snippetModel){
        var codeGraph = new SnippetCodeGraph(snippetModel);
        if(string.IsNullOrEmpty(codeGraph.Body.NamespaceName)){
            Console.WriteLine("No namespace found");

            }
        var import = new Dictionary<string, string>(){
            {"Name", codeGraph.Body.Name},
            {"TypeDefinition", codeGraph.Body.TypeDefinition},
            {"NamespaceName", codeGraph.Body.NamespaceName}
        };
        imports.Add(import);
        if (codeGraph.Body.Children.Count > 0){
            foreach ( var child in codeGraph.Body.Children)
            {
                if(child.NamespaceName != null){
                    var childimport = new Dictionary<string, string>(){
                        {"Name", child.Name},
                        {"TypeDefinition", child.TypeDefinition},
                        {"NamespaceName", child.NamespaceName}
                    };
                    imports.Add(childimport);
                }
                if (child.Children.Count > 0){
                    foreach ( var grandchild in child.Children)
                    {
                        if(grandchild.NamespaceName != null){
                            var grandchildimport = new Dictionary<string, string>(){
                                {"Name", grandchild.Name},
                                {"TypeDefinition", grandchild.TypeDefinition},
                                {"NamespaceName", grandchild.NamespaceName}
                            };

                            imports.Add(grandchildimport);
                        }
                        if(grandchild.Children != null){
                            foreach ( var grandchild2 in grandchild.Children)
                            {
                                if(grandchild2.NamespaceName != null){
                                    var grandchildimport = new Dictionary<string, string>(){
                                        {"Name", grandchild2.Name},
                                        {"TypeDefinition", grandchild2.TypeDefinition},
                                        {"NamespaceName", grandchild2.NamespaceName}
                                    };
                                  
                                    imports.Add(grandchildimport);
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
    



