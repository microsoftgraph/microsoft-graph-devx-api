using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public enum ImportKind
    {
        Path,
        RequestBuilder,
        Model
    }

    public class ImportTemplate
    {
        public ImportKind Kind
        {
            get; set;
        }
        public CodeProperty ModelProperty
        {
            get; set;
        }
        public string Path
        {
            get; set;
        }
        public string RequestBuilderName
        {
            get; set;
        }
    }

    public class ImportsGenerator
    {
        public List<ImportTemplate> imports = new();

        public List<ImportTemplate> GenerateImportTemplates(SnippetModel snippetModel)
        {
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var imports = new List<ImportTemplate>();
            if (codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions())
            {
                var className = codeGraph.Nodes.Last().GetClassName().ToFirstCharacterUpperCase();
                var itemSuffix = codeGraph.Nodes.Last().Segment.IsCollectionIndex() ? "Item" : string.Empty;
                var requestBuilderName = $"{className}{itemSuffix}RequestBuilder";
                if (codeGraph.Nodes.Last().Path != null)
                {
                    var path = codeGraph.Nodes.Last().Path;
                    imports.Add(new ImportTemplate
                    {
                        Kind = ImportKind.Path,
                        Path = Regex.Replace(path.Replace("\\", ".").Replace("()", ""), @"\{[^}]*-id\}", "item", RegexOptions.Compiled, TimeSpan.FromSeconds(60)),
                        RequestBuilderName = requestBuilderName
                    });
                }
            }
            AddModelImportTemplates(codeGraph.Body, imports);
            return imports;
        }

        private static void AddModelImportTemplates(CodeProperty node, List<ImportTemplate> imports)
        {
            if (!string.IsNullOrEmpty(node.NamespaceName))
            {
                imports.Add(new ImportTemplate
                {
                    Kind = ImportKind.Model,
                    ModelProperty = node
                });
            }

            if (node.Children is { Count: > 0 })
            {
                foreach (var child in node.Children)
                {
                    AddModelImportTemplates(child, imports);
                }
            }
        }
    }
}
