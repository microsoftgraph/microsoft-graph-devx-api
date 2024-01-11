using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using CodeSnippetsReflection.StringExtensions;


namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class GetImports
    {
        private List<string> importPaths;
        private string importPrefix;

        private static readonly String[] NonModelDeclarationSuffixes = new String[]{"RequestConfiguration", "QueryParameters", "RequestBody", "RequestBuilder"};
        private static readonly Regex RequestExecutorLineRegex = new Regex(@"await graph_client\.(.+)\.", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex ModelDeclarationRegex = new Regex(@"[=,\[]\s*([A-Z]\w+)\(", RegexOptions.Compiled);
        private static readonly Regex EnumsAndRequestBuilderDeclarationRegex = new Regex(@"[=,\[]\s*([A-Z]\w+)\.", RegexOptions.Compiled); //Enums and RequestBuilders
        private static readonly HashSet<string> PythonStandardTypes = new HashSet<string>{"base64"};
        private static readonly Regex NestedModelNamespaceImportErrorRegex = new Regex(@"Unable to import.+'\w+\.generated\.models\.(\w+)\.(\w+)'.+import-error", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly HashSet<string> PythonInternalTypes = new HashSet<string>{"UUID"};


    
    
        static bool IsNonModelClass(string input)
        {
            foreach (string suffix in NonModelDeclarationSuffixes)
            {
                if (input.EndsWith(suffix))
                {
                    return true;
                }
            }
            return false;
        }

        static bool IsModelClass(string input)
        {
            return ModelDeclarationRegex.IsMatch(input);
        }

        public GetImports()
        {
            importPaths = new List<string>();
            importPrefix = "from msgraph";
            importPaths.Add($"{importPrefix} import GraphServiceClient");
        }

        // Method to infer import statements based on snippet text
    public string GenerateImportStatements(string snippetText)
    {
        var modelDeclarations = ModelDeclarationRegex.Matches(snippetText);
        var enumsAndRequestBuilderDeclarations = EnumsAndRequestBuilderDeclarationRegex.Matches(snippetText);

        IEnumerable<Match> combinedDeclarations = modelDeclarations.OfType<Match>()
                                                    .Concat(enumsAndRequestBuilderDeclarations.OfType<Match>())
                                                    .Where(m => m.Success);


        var declarationNames = new HashSet<string>(
            combinedDeclarations.Where(match => !String.Equals("GraphServiceClient", match.Groups[1].Value, StringComparison.OrdinalIgnoreCase)
                                        && !match.Groups[1].Value.StartsWith('.')) // Ignore relative imports for built in types
                        .Select(match => match.Groups[1].Value)
        );
        Console.WriteLine("Declaration Names: " + declarationNames);
        if (declarationNames.Any())
            {
                var importNamespacePaths = InferRequestBuilderNamespacePath(snippetText);
                var requestBuilderImportNamespaceStr = String.Join(".", importNamespacePaths);
                Console.WriteLine($"Request Builder import namespace {requestBuilderImportNamespaceStr}");

                var uniqueImportPaths = new HashSet<string>(importNamespacePaths);
                Console.WriteLine($"Unique import paths {uniqueImportPaths}");

                foreach (string declarationName in declarationNames)
                {
                    // Custom Kiota types
                    if (PythonStandardTypes.Contains(declarationName))
                        {
                            Console.WriteLine($"import {declarationName}");
                            importPaths.Add($"import {declarationName}");
                    }
                    else if (PythonInternalTypes.Contains(declarationName)) {
                    importPaths.Add($"from {declarationName.ToLowerInvariant()} import {declarationName}");
                    }
                    else if (IsNonModelClass(declarationName)) {
                        
                        Console.WriteLine($"import {declarationName}");
                        importPaths.Add($"{importPrefix}.generated.{requestBuilderImportNamespaceStr}.{declarationName.ToSnakeCase()} import {declarationName}");
                }
                    else if (IsModelClass(declarationName)){
                        // No nesting yet
                    Console.WriteLine($"import {declarationName}");
                    importPaths.Add($"{importPrefix}.generated.models.{declarationName.ToSnakeCase()} import {declarationName}");// check out for nesting
                    }                       
                }
            }
            else
            {
                Console.WriteLine("No declaration names");
            }

        return string.Join("\n", importPaths);
    }

        // Method to generate import statements in new lines
        public string GenerateImports()
        {
            if (importPaths.Count == 0)
            {
                return "No imports added.";
            }

            string imports = "";

            foreach (var path in importPaths)
            {
                imports += $"{path}\n";
            }

            return imports;
        }

        private static IEnumerable<string> InferRequestBuilderNamespacePath(string snippetText)
    {
        var requestExecutorMatch = RequestExecutorLineRegex.Match(snippetText);
        if (!requestExecutorMatch.Success)
        {
            throw new ArgumentException($"Snippet does NOT contain request executor line: {snippetText}");
        }
        var requestBuilderMethods = requestExecutorMatch.Groups[1].Value.Split(".")
        .Select(methodName =>
        {
            int indexOfOpenParenthesis = methodName.IndexOf("(", StringComparison.InvariantCulture);
            return indexOfOpenParenthesis >= 0 ? methodName[..indexOfOpenParenthesis] : methodName;
        })
        .ToList();
            Console.WriteLine($"Request Builder Methods {requestBuilderMethods}");

        if (!requestBuilderMethods.Any())
        {
           // Assert.Fail($"Request executor line does NOT contain request builder methods: {requestExecutorMatch.Groups[1].Value}");
           Console.WriteLine($"Request executor line does NOT contain request builder methods: {requestExecutorMatch.Groups[1].Value}");
        }
        List<string> importNamespaceValues = new List<string>();
        foreach (string methodName in requestBuilderMethods)
        {
            var itemPrefix = "by";
            // Append "Item" to namespace if request builder method contains prefix
            if (methodName.StartsWith(itemPrefix, StringComparison.OrdinalIgnoreCase)) {
                importNamespaceValues.Add("item");
                continue;
            }
            if (methodName.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                importNamespaceValues.AddRange(new List<string>{"users", "item"});
                continue;
            }
            importNamespaceValues.Add(methodName.ToLowerInvariant());
        }

        return importNamespaceValues;
    }
        
    }
}
