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
        // Call recursive function to get all model import to be added to the template
        var imports = new List<Dictionary<string, string>>();
        AddModelImportTemplates(codeGraph.Body, imports);
        
        return imports;
        }
    private static void AddModelImportTemplates(CodeProperty node, List<Dictionary<string, string>> imports)
        {
            if (!string.IsNullOrEmpty(node.NamespaceName))
            {
                var import = new Dictionary<string, string>
                {
                    { "Name", node.Name },
                    { "TypeDefinition", node.TypeDefinition },
                    { "NamespaceName", node.NamespaceName }
                };
                imports.Add(import);
            }

            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    AddModelImportTemplates(child, imports);
                }
            }
        }
        
    }
    



