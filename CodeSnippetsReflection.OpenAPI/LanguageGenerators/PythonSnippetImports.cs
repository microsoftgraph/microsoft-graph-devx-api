using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using CodeSnippetsReflection.StringExtensions;
using System.Threading;



namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class GetImports
    {
        private List<string> importPaths;
        private string importPrefix;

        private static readonly String[] NonModelDeclarationSuffixes = new String[]{"RequestConfiguration", "QueryParameters", "RequestBody", "RequestBuilder"};
        private static readonly Regex RequestExecutorLineRegex = new Regex(@"await graph_client\.(.+)\.", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromSeconds(60));
        private static readonly Regex ModelDeclarationRegex = new Regex(@"[=,\[]\s*([A-Z]\w+)\(", RegexOptions.Compiled, TimeSpan.FromSeconds(60));
        private static readonly Regex EnumsAndRequestBuilderDeclarationRegex = new Regex(@"[=,\[]\s*([A-Z]\w+)\.", RegexOptions.Compiled, TimeSpan.FromSeconds(60)); //Enums and RequestBuilders
        private static readonly HashSet<string> PythonStandardTypes = new HashSet<string>{"base64"};
        private static readonly HashSet<string> PythonInternalTypes = new HashSet<string>{"UUID"};
        

        private static readonly Dictionary<string, string> PathItemToNestedModelNamespace = new Dictionary<string, string>{
        {"term_store", "term_store"},
        {"call_records", "call_records"},
        {"external_connectors", "external_connectors"},
        {"external", "external_connectors"},
        {"security", "security"},
        {"ediscovery", "ediscovery"},
        {"identity_governance", "identity_governance"},
        {"industry_data", "industry_data"},
        {"updates", "windows_updates"},
        {"device_management", "device_management"},
        {"managed_tenants", "managed_tenants"},
        {"search", "search"}
    };



    
    
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
                var uniqueImportPaths = new HashSet<string>(importNamespacePaths);
                foreach (string declarationName in declarationNames)
                {
                    // Custom Kiota types
                    if (PythonStandardTypes.Contains(declarationName))
                        {
                            importPaths.Add($"import {declarationName}");
                    }
                    else if (PythonInternalTypes.Contains(declarationName)) {
                    importPaths.Add($"from {declarationName.ToLowerInvariant()} import {declarationName}");
                    }
                    else if (IsNonModelClass(declarationName)) {
                        
                        importPaths.Add($"{importPrefix}.generated.{requestBuilderImportNamespaceStr}.{declarationName.ToSnakeCase()} import {declarationName}");
                }
                    else if (IsModelClass(declarationName)){
                    importPaths.Add($"{importPrefix}.generated.models.{declarationName.ToSnakeCase()} import {declarationName}");// check out for nesting
                    }
                    else{
                        var nestedModelPath = new List<string>();
                        foreach (var path in uniqueImportPaths)
                        {
                            if (PathItemToNestedModelNamespace.ContainsKey(path))
                            {
                                nestedModelPath.Add(path);
                            }
                        }
                        if (nestedModelPath.Count > 0) {    
                            var nestedModelNamespace = PathItemToNestedModelNamespace.TryGetValue(nestedModelPath.Last(), out var value) ? value : null;
                            importPaths.Add($"{importPrefix}.generated.models.{nestedModelNamespace.ToSnakeCase()}.{declarationName.ToSnakeCase()} import {declarationName}");
                        }
                        else{
                            importPaths.Add($"{importPrefix}.generated.models.{declarationName.ToSnakeCase()} import {declarationName}");
                        }
                    }                       
                }
            }
        return string.Join("\n", importPaths);
    }        
        private static IEnumerable<string> InferRequestBuilderNamespacePath(string snippetText)
    {
        var requestExecutorMatch = RequestExecutorLineRegex.Match(snippetText, Timeout.Infinite);
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

        if (requestBuilderMethods.Count == 0)
        {
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
