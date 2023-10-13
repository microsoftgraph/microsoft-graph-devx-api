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
        private const string HttpCoreVarName = "request_adapter";
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
            var snippetBuilder = new StringBuilder($"# THE PYTHON SDK IS IN PREVIEW. FOR NON-PRODUCTION USE ONLY{Environment.NewLine}{Environment.NewLine}" +
                                                   $"{ClientVarName} = {ClientVarType}({HttpCoreVarName}){Environment.NewLine}{Environment.NewLine}");

            WriteRequestPayloadAndVariableName(codeGraph, snippetBuilder, indentManager);
            WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
            return snippetBuilder.ToString();
        }

        private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            var method = codeGraph.HttpMethod.Method.ToLower();
            var configParameter = codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions()
                ? $"{RequestConfigurationVarName} = {RequestConfigurationVarName}"
                : string.Empty;
            var bodyParameter = codeGraph.HasBody()
                ? $"body = {RequestBodyVarName}"
                : string.Empty;
            var optionsParameter = codeGraph.HasOptions() ? "options =" : string.Empty;
            var returnVar = codeGraph.HasReturnedBody() ? "result = " : string.Empty;
            var parameterList = GetActionParametersList(bodyParameter, configParameter, optionsParameter);
            snippetBuilder.AppendLine(GetRequestConfiguration(codeGraph, indentManager));
            snippetBuilder.AppendLine($"{returnVar}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{method}({parameterList})");
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
            var requestBuilderName = codeGraph.Nodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase();
            var className = $"{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            
            var classNameQueryParameters = $"{requestBuilderName}.{requestBuilderName}{codeGraph.HttpMethod.Method.ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            
            var queryParamsPayload = GetRequestQueryParameters(codeGraph, indentManager, classNameQueryParameters);
            if (codeGraph.HasParameters() || codeGraph.HasHeaders()){
                snippetBuilder.AppendLine(queryParamsPayload); 
                snippetBuilder.AppendLine($"{RequestConfigurationVarName} = {requestBuilderName}.{className}(");
                indentManager.Indent(); 

                var requestHeadersPayload = GetRequestHeaders(codeGraph, indentManager);
                if (codeGraph.HasHeaders()){
                    if(queryParamsPayload != null){
                        snippetBuilder.AppendLine($"query_parameters = {RequestParametersPropertyName},");
                    }
                    snippetBuilder.AppendLine(requestHeadersPayload);
                    indentManager.Unindent();
                    snippetBuilder.AppendLine(")");
                }         
                if (!codeGraph.HasHeaders()){
                    snippetBuilder.AppendLine($"query_parameters = {RequestParametersPropertyName},");
                    indentManager.Unindent();
                    snippetBuilder.AppendLine(")");
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
        
        private static string GetRequestHeaders(SnippetCodeGraph snippetModel, IndentManager indentManager)
        {
            var headersvar = new StringBuilder();
            if (!snippetModel.HasHeaders())
                headersvar.AppendLine("headers = {}");
            headersvar.AppendLine("headers = {");
            indentManager.Indent();
            foreach (var header in snippetModel.Headers)
            {
                headersvar.AppendLine($"{indentManager.GetIndent()}'{header.Name}' : {EvaluateParameter(header)},");
            }
            indentManager.Unindent();
            headersvar.AppendLine("}");
            return headersvar.ToString();
    
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
            if (propertyName!= null && propertyName.Equals("additional_data", StringComparison.OrdinalIgnoreCase))
            {
                fromAdditionalData = true;
            }

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
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}]");
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

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            if (!(nodes?.Any() ?? false)) return string.Empty;
            var elements = nodes.Select(static (x, i) =>
                {
                    if (x.Segment.IsCollectionIndex())
                        return $"by_type_id{x.Segment.Replace("{", "('").Replace("}", "')")}";
                    else if (x.Segment.IsFunction())
                        return x.Segment.Replace(".", "_").RemoveFunctionBraces().Split('.')
                            .Select(static s => s.ToSnakeCase())
                            .Aggregate(static (a, b) => $"{a}{b}");
                    return x.Segment.ReplaceValueIdentifier().TrimStart('$').RemoveFunctionBraces()
                        .ToSnakeCase();
                })
                .Aggregate(new List<String>(), (current, next) =>
                {
                    var element = next.Contains("by_type_id", StringComparison.OrdinalIgnoreCase)
                        ? next.Replace("by_type_id",
                            $"by_{current.Last().Replace("s().", string.Empty, StringComparison.OrdinalIgnoreCase)}_id")
                        : $"{next.Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase).ToFirstCharacterLowerCase()}";

                    current.Add(element);
                    return current;
                }).Aggregate(static (x, y) =>
                {
                    return $"{x.Trim('$').Replace("s_", "_")}.{y.Trim('$', '.').Replace("()", string.Empty).Replace("s_", "_")}";
                }).Replace("..", ".")
                .Replace("().", ".")
                .Replace("()().", ".");
                

            return string.Join("", elements).Replace("()()", "()");

        }
    }
}
