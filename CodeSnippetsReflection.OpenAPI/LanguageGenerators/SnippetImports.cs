using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class ImportsGenerator
{   
    /// <summary>
    /// Generates a list of import templates for a given snippet model.
    /// </summary>
    /// <param name="snippetModel">The snippet model for which to generate import templates.</param>
    /// <returns>A list of dictionaries, each representing an import template with keys "Path" and "RequestBuilderName".</returns>
    /// <remarks>
    /// This method first creates a code graph from the snippet model. If the code graph has headers, parameters, or options, 
    /// it generates a request builder name and potentially modifies it based on the last path part of the last node in the code graph. 
    /// It then adds a new import template to the list of imports. Finally, it calls `AddModelImportTemplates` to add more import templates based on the body of the code graph.
    /// </remarks>
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
                        { "Path", Regex.Replace(path.Replace("\\", ".").Replace("()", ""), @"\{[^}]*-id\}", "item", RegexOptions.Compiled, TimeSpan.FromSeconds(60))},
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

    /// <summary>
    /// Adds model import templates to the provided list based on the given code property node.
    /// </summary>
    /// <param name="node">The code property node from which to generate import templates.</param>
    /// <param name="imports">The list to which import templates will be added.</param>
    /// <param name="requestBuilderName">The name of the request builder, if any. Default is null.</param>
    /// <param name="path">The path for the import, if any. Default is null.</param>
    /// <remarks>
    /// This method checks if the namespace name of the node is not empty. If it's not, it creates a new import template dictionary 
    /// with keys "Name", "TypeDefinition", "NamespaceName", "PropertyType", and "Value", and adds this dictionary to the provided list of imports.
    /// </remarks>
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
                    {"Value", node.Value}

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




