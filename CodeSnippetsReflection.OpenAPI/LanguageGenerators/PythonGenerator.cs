using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class PythonGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string ClientVarName = "graph_client";
        private const string ClientVarType = "GraphServiceClient";
        private const string CredentialVarName = "credentials";
        private const string ScopesVarName = "scopes";
        private const string RequestBodyVarName = "request_body";
        private const string RequestConfigurationVarName = "request_configuration";
        private const string RequestParametersPropertyName = "query_params";
        
        private static readonly HashSet<string> ReservedTypeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "and",
            "as",
            "assert",
            "async",
            "await",
            "break",
            "class",
            "continue",
            "def",
            "del",
            "dict",
            "elif",
            "else",
            "except",
            "finally",
            "False",
            "for",
            "from",
            "global",
            "if",
            "import",
            "in",
            "is",
            "lambda",
            "list",
            "nonlocal",
            "None",
            "not",
            "or",
            "pass",
            "raise",
            "return",
            "True",
            "try",
            "with",
            "while",
            "yield",
            "property",
        };
        
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder($"{Environment.NewLine}{Environment.NewLine}" +
                                                   $"{ClientVarName} = {ClientVarType}({CredentialVarName}, {ScopesVarName}){Environment.NewLine}{Environment.NewLine}");

            WriteRequestPayloadAndVariableName(codeGraph, snippetBuilder, indentManager);
            WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
            var importStatements = GetImportStatements(snippetModel);
            snippetBuilder.Insert(0, string.Join(Environment.NewLine, importStatements));
            return snippetBuilder.ToString();
        }
        private static HashSet<string> GetImportStatements(SnippetModel snippetModel){
            string modelImportPrefix = "from msgraph.generated.models";
            string requestBuilderImportPrefix = "from msgraph.generated";
        
            HashSet<string> snippetImports = new HashSet<string>();

            snippetImports.Add("from msgraph import GraphServiceClient");

            var  _importsGenerator = new ImportsGenerator();
            var imports = _importsGenerator.GenerateImportTemplates(snippetModel);
            foreach(var import in imports){
                if (import.ContainsKey("NamespaceName") && string.IsNullOrEmpty(import["NamespaceName"]))
                    continue;
                if (import.ContainsKey("NamespaceName") &&   import["NamespaceName"].Contains("models")) {
                    snippetImports.Add($"{modelImportPrefix}.{import["Name"].ToSnakeCase()} import {import["Name"]}");
                }
                if(import.ContainsKey("NamespaceName") && !import["NamespaceName"].Contains("models")){
                    // import and path to request Body
                    snippetImports.Add($"{requestBuilderImportPrefix}.{string.Join(".", import["NamespaceName"].Split('.').Select((s, i) => i == import["NamespaceName"].Split('.').Length - 1 ? s.ToSnakeCase() : s.ToLowerInvariant()))}.{import["Name"].ToSnakeCase()} import {import["Name"]}");                    
                    //import["NamespaceName"].EndsWith("RequestBuilder") ||
                }
                if(import.ContainsKey("NamespaceName") && !import["NamespaceName"].Contains("models") && import["RequestBuilderName"] != null){
                    // import and path to request builder
                    snippetImports.Add($"{requestBuilderImportPrefix}.{string.Join(".", import["NamespaceName"].Split('.').Select((s, i) => i == import["NamespaceName"].Split('.').Length - 1 ? s.ToSnakeCase() : s.ToLowerInvariant()))}.{import["RequestBuilderName"].ToSnakeCase()} import {import["Name"]}");                    
                }

            }

            return snippetImports;
        }

        private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            var method = codeGraph.HttpMethod.Method.ToLower();
            var configParameter = codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions()
                ? $"{RequestConfigurationVarName} = {RequestConfigurationVarName}"
                : string.Empty;
            var bodyParameter = codeGraph.HasBody()
                ? $"{RequestBodyVarName}"
                : string.Empty;
            if (string.IsNullOrEmpty(bodyParameter) && ((codeGraph.RequestSchema?.Properties?.Any() ?? false) || (codeGraph.RequestSchema?.AllOf?.Any(schema => schema.Properties.Any()) ?? false)))
                bodyParameter = "None";// pass a null parameter if we have a request schema expected but there is not body provided
            
            var optionsParameter = codeGraph.HasOptions() ? "options =" : string.Empty;
            var returnVar = codeGraph.HasReturnedBody() ? "result = " : string.Empty;
            string pathSegment = $"{ClientVarName}.{GetFluentApiPath(codeGraph.Nodes, codeGraph)}";
            var parameterList = GetActionParametersList(bodyParameter, configParameter, optionsParameter);
            snippetBuilder.AppendLine(GetRequestConfiguration(codeGraph, indentManager));
            snippetBuilder.AppendLine($"{returnVar}await {pathSegment}.{method}({parameterList})");
        }
        private static string GetRequestQueryParameters(SnippetCodeGraph model, IndentManager indentManager, string classNameQueryParameters)
        {
            var snippetBuilder = new StringBuilder();
            if (!model.HasParameters())
                return default;
            snippetBuilder.AppendLine($"{RequestParametersPropertyName} = {classNameQueryParameters}(");
            indentManager.Indent(2);
            foreach (var queryParam in model.Parameters)
            {
                var queryParameterName = NormalizeQueryParameterName(queryParam.Name.ToSnakeCase()).ToFirstCharacterLowerCase();
                snippetBuilder.AppendLine($"{indentManager.GetIndent()}{queryParameterName} = {EvaluateParameter(queryParam)},");
            }
            indentManager.Unindent();
            snippetBuilder.AppendLine(")");
            return snippetBuilder.ToString();
        }
        private static string EvaluateParameter(CodeProperty param)
        {
            return param.PropertyType switch
            {
                PropertyType.Array =>
                    $"[{string.Join(",", param.Children.Select(static x => $"\"{x.Value}\"").ToList())}]",
                PropertyType.Boolean => param.Value.ToFirstCharacterUpperCase(),
                PropertyType.Int32 or PropertyType.Double or PropertyType.Float32 or PropertyType.Float64 or PropertyType.Int64 => param.Value,
                _ => $"\"{param.Value.EscapeQuotes()}\""
            };
        }
        private static string GetRequestConfiguration(SnippetCodeGraph codeGraph, IndentManager indentManager)
        {
            var snippetBuilder = new StringBuilder();
            var className = codeGraph.Nodes.Last().GetClassName().ToFirstCharacterUpperCase();
            var itemSuffix = codeGraph.Nodes.Last().Segment.IsCollectionIndex() ? "Item" : string.Empty;
            var requestBuilderName = $"{className}{itemSuffix}RequestBuilder";
            var requestConfigurationName =
                $"{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            
            var classNameQueryParameters = $"{requestBuilderName}.{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            
            var queryParamsPayload = GetRequestQueryParameters(codeGraph, indentManager, classNameQueryParameters);
            if (codeGraph.HasParameters() || codeGraph.HasHeaders()){
                snippetBuilder.AppendLine(queryParamsPayload); 
                snippetBuilder.Append($"{RequestConfigurationVarName} = {requestBuilderName}.{requestConfigurationName}(");

                if (codeGraph.HasParameters()){
                    if (!string.IsNullOrEmpty(queryParamsPayload))
                    {
                        snippetBuilder.AppendLine();
                        indentManager.Indent();
                        snippetBuilder.AppendLine($"query_parameters = {RequestParametersPropertyName},");
                        indentManager.Unindent();
                    }
                    snippetBuilder.AppendLine(")");
                }         
                if (codeGraph.HasHeaders())
                {
                    if (!codeGraph.HasParameters())
                        snippetBuilder.AppendLine(")");
                    snippetBuilder.AppendLine(GetRequestHeaders(codeGraph));
                }
                
            }    
            return snippetBuilder.ToString();
        }
        private static string GetActionParametersList(params string[] parameters) {
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
            var emptyParameters = nonEmptyParameters.ToList();
            if(emptyParameters.Any())
                return string.Join(", ", emptyParameters.Select(static x => $"{x}").Aggregate(static (a, b) => $"{a}, {b}"));
            return string.Empty;
        }
        
        private static string GetRequestHeaders(SnippetCodeGraph snippetModel)
        {
            var headersVar = new StringBuilder();
            foreach (var header in snippetModel.Headers)
            {
                headersVar.AppendLine(
                    $"{RequestConfigurationVarName}.headers.add(\"{header.Name}\", {EvaluateParameter(header)})");
            }
            return headersVar.ToString();
    
        }
        private static string NormalizeQueryParameterName(string queryParam) => queryParam?.TrimStart('$').ToFirstCharacterLowerCase();

        private static void WriteRequestPayloadAndVariableName(SnippetCodeGraph snippetCodeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasBody())
                return;// No body

            if(indentManager == null) 
                throw new ArgumentNullException(nameof(indentManager));

            switch (snippetCodeGraph.Body.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"{RequestBodyVarName} = {GetTypeString(snippetCodeGraph.Body)}");
                    snippetCodeGraph.Body.Children.ForEach( child => WriteObjectFromCodeProperty(snippetCodeGraph.Body, child, snippetBuilder, indentManager));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()})");
                    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"{RequestBodyVarName} = BytesIO()"); // stream to upload
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type for request: {snippetCodeGraph.Body.PropertyType}");
            }
        }

        private static void WriteObjectFromCodeProperty(CodeProperty parentProperty, CodeProperty codeProperty,StringBuilder snippetBuilder, IndentManager indentManager, bool fromAdditionalData = false) 
        {
            indentManager.Indent();
            var isParentArray = parentProperty.PropertyType == PropertyType.Array;
            var isParentMap = parentProperty.PropertyType == PropertyType.Map;
            var assignmentSuffix = ",";
            var propertyName = codeProperty.Name?.CleanupSymbolName()?.ToSnakeCase();
            fromAdditionalData = fromAdditionalData || (propertyName != null &&
                                                        propertyName.Equals("additional_data",
                                                            StringComparison.OrdinalIgnoreCase));
            if (fromAdditionalData && codeProperty.PropertyType==PropertyType.Object)
            {
                codeProperty.PropertyType = PropertyType.Map;
            }
            var propertyAssignment = $"{indentManager.GetIndent()}{propertyName} = "; // default assignments to the usual "x = xyz"
            if (isParentMap)
            {
                propertyAssignment = $"{indentManager.GetIndent()}\"{codeProperty.Name.ToSnakeCase()}\" : "; // if its in the additionalData assignments happen using string value keys
            }
            else if (isParentArray)
            {
                propertyAssignment = $"{indentManager.GetIndent()}"; // no assignments as entries as added directly to the collection/array
            }

            switch (codeProperty.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"{propertyAssignment}{GetTypeString(codeProperty)}");
                    codeProperty.Children.ForEach( child => WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager, fromAdditionalData));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}){assignmentSuffix}");
                    break;
                case PropertyType.Map:
                    snippetBuilder.AppendLine($"{propertyAssignment}{GetTypeString(codeProperty)}");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager, fromAdditionalData);
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{((isParentArray || isParentMap) ? "," : string.Empty)}");
                    break;
                case PropertyType.Array :
                    snippetBuilder.AppendLine($"{propertyAssignment}{GetTypeString(codeProperty)}");
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager, fromAdditionalData);
                    });
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}],");
                    break;
                case PropertyType.Guid:
                    snippetBuilder.AppendLine($"{propertyAssignment}UUID(\"{codeProperty.Value}\"){assignmentSuffix}");
                    break;
                case PropertyType.String:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\"{assignmentSuffix}");
                    break;
                case PropertyType.Enum:
                    var enumTypeString = GetTypeString(codeProperty);
                    var enumValues = codeProperty.Value.Split(new []{'|',','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            var enumHint = x.Split('.').Last().Trim();
                            // the enum member may be invalid so default to generating the first value in case a look up fails.
                            var enumMember = codeProperty.Children.FirstOrDefault( member => member.Value.Equals(enumHint,StringComparison.OrdinalIgnoreCase)).Value ?? codeProperty.Children.FirstOrDefault().Value ?? enumHint;
                            return $"{enumTypeString.TrimEnd('?')}.{enumMember.ToFirstCharacterUpperCase()}";
                        })
                        .Aggregate(static (x, y) => $"{x} | {y}");
                    snippetBuilder.AppendLine($"{propertyAssignment}{enumValues}{assignmentSuffix}");
                    break;
                case PropertyType.Base64Url:
                    snippetBuilder.AppendLine($"{propertyAssignment}base64.urlsafe_b64decode(\"{codeProperty.Value.EscapeQuotes()}\"){assignmentSuffix}");
                    break;
                case PropertyType.Null:
                    snippetBuilder.AppendLine($"{propertyAssignment}None{assignmentSuffix}");
                    break;
                case PropertyType.Boolean:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value.ToFirstCharacterUpperCase()}{assignmentSuffix}");
                    break;
                case PropertyType.Int32:
                case PropertyType.Int64:
                case PropertyType.Double:
                case PropertyType.Float32:
                case PropertyType.Float64:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}{assignmentSuffix}");
                    break;
                case PropertyType.Binary:
                case PropertyType.Default:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\"{assignmentSuffix}");
                    break;
                default:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\"{assignmentSuffix}");
                    break;
            }
            indentManager.Unindent();
        }
        private static string GetTypeString(CodeProperty codeProperty)
        {
            var typeString = codeProperty.TypeDefinition.ToFirstCharacterUpperCase() ??
                             codeProperty.Value.ToFirstCharacterUpperCase();
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Array:
                    return $"[";
                case PropertyType.Object:
                    return $"{ReplaceIfReservedTypeName(typeString)}(";
                case PropertyType.Map:
                    return "{";
                case PropertyType.Enum:
                    return $"{ReplaceIfReservedTypeName(typeString.Split('.').First())}?";
                default:
                    return string.Empty;
            }
        }
        private static string ReplaceIfReservedTypeName(string originalString, string suffix = "_")
            => ReservedTypeNames.Contains(originalString) ? $"{originalString}{suffix}" : originalString;

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes, SnippetCodeGraph snippetCodeGraph, bool useIndexerNamespaces = false)
        {
            if(!(nodes?.Any() ?? false)) 
                return string.Empty;

            return nodes.Select(x => {
                                        if(x.Segment.IsCollectionIndex())
                                        {
                                            var collectionIndexName = x.Segment.Replace("{", "").Replace("}", "");
                                            var fluentMethodName = collectionIndexName.Split("-").Select(static x => x.ToSnakeCase()).Aggregate(static (a, b) => a + $"_{b}");
                                            return $"by_{fluentMethodName}('{collectionIndexName}')";
                                        }
                                        if (x.Segment.IsFunctionWithParameters())
                                        {
                                            var functionName = x.Segment.Split('(').First();
                                            functionName = functionName.Split(".",StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(static s => s.ToFirstCharacterUpperCase())
                                                                        .Aggregate(static (a, b) => $"{a}{b}")
                                                                        .ToSnakeCase();
                                            var parameters = snippetCodeGraph.PathParameters
                                                .Select(static s => $"_with_{s.Name.ToSnakeCase()}")
                                                .Aggregate(static (a, b) => $"{a}{b}");

                                            // use the existing WriteObjectFromCodeProperty functionality to write the parameters as if they were a comma seperated array so as to automatically infer type handling from the codeDom :)
                                            var parametersBuilder = new StringBuilder();
                                            foreach (var codeProperty in snippetCodeGraph.PathParameters.OrderBy(static parameter => parameter.Name, StringComparer.OrdinalIgnoreCase))
                                            {
                                                var parameter = new StringBuilder();
                                                WriteObjectFromCodeProperty(new CodeProperty{PropertyType = PropertyType.Array}, codeProperty, parameter, new IndentManager());
                                                parametersBuilder.Append(parameter.ToString().Trim());//Do this to trim the surrounding whitespace generated
                                            }
                                            
                                            return functionName
                                                   + parameters
                                                   + $"({parametersBuilder.ToString().TrimEnd(',')})" ;
                                        }
                                        if (x.Segment.IsFunction())
                                            return x.Segment.RemoveFunctionBraces().Split('.')
                                                .Select(static s => s.ToFirstCharacterUpperCase())
                                                .Aggregate(static (a, b) => $"{a}{b}")
                                                .ToSnakeCase();
                                        return x.Segment.ReplaceValueIdentifier().TrimStart('$').RemoveFunctionBraces().ToSnakeCase();
                                      })
                                .Aggregate(static (x, y) =>
                                {
                                    var dot = y.StartsWith("[") ? string.Empty : ".";
                                    return $"{x}{dot}{y}";
                                });
        }
    }
}
