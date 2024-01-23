using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph;

public class ImportsGenerator
{
    public List<Dictionary<string, string>> imports = new();
    public List<Dictionary<string, string>> GenerateImportTemplates(SnippetModel snippetModel)
    {
        var codeGraph = new SnippetCodeGraph(snippetModel);
        // Call recursive function to get all model import to be added to the template
        var imports = new List<Dictionary<string, string>>();
        if (codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions())
        {
            // we have a request builder
            var className = codeGraph.Nodes.Last().GetClassName().ToFirstCharacterUpperCase();
            var itemSuffix = codeGraph.Nodes.Last().Segment.IsCollectionIndex() ? "Item" : string.Empty;
            var requestBuilderName = $"{className}{itemSuffix}RequestBuilder";
            //check path and include last part to request builder name
            if (codeGraph.Nodes.Last().Path != null)
            {
                var path = codeGraph.Nodes.Last().Path;
                var pathParts = path.Split('/');
                var lastPathPart = pathParts.Last();
                if (lastPathPart != null)
                {
                    requestBuilderName = $"{requestBuilderName}";
                    imports.Add(new Dictionary<string, string>
                    {
                        { "Path", Regex.Replace(path.Replace("\\", ".").Replace("()", ""), @"\{[^}]*-id\}", "item", RegexOptions.None, TimeSpan.FromSeconds(60))},
                        { "RequestBuilderName", requestBuilderName}
                    });
                }

            }
            // request builder name exists, call recursive function with request builder name, default null

        }
        // else call the normal recursive function without request builder name
        AddModelImportTemplates(codeGraph.Body, imports);


        return imports;
    }
    private static void AddModelImportTemplates(CodeProperty node, List<Dictionary<string, string>> imports, string requestBuilderName = null, string path = null)
    {
        if (!string.IsNullOrEmpty(node.NamespaceName))
        {
            var import = new Dictionary<string, string>
                {
                    { "Name", node.Name },
                    { "TypeDefinition", node.TypeDefinition },
                    { "NamespaceName", node.NamespaceName },
                    {"PropertyType", node.PropertyType.ToString()},
                    {"Value", node.Value},

                };
            imports.Add(import);
        }

        if (node.Children != null && node.Children.Count > 0)
        {
            foreach (var child in node.Children)
            {
                AddModelImportTemplates(child, imports, requestBuilderName, path);
            }
        }
    }

}




