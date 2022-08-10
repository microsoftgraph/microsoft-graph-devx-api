using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string ClientVarName = "graphClient";
        private const string ClientVarType = "GraphServiceClient";
        private const string HttpCoreVarName = "requestAdapter";
        private const string RequestBodyVarName = "requestBody";
        private const string RequestConfigurationVarName = "requestConfiguration";
        private const string RequestHeadersPropertyName = "Headers";
        private const string RequestParametersPropertyName = "QueryParameters";

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder(
                                    "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                                    $"var {ClientVarName} = new {ClientVarType}({HttpCoreVarName});{Environment.NewLine}{Environment.NewLine}");

            WriteRequestPayloadAndVariableName(codeGraph, snippetBuilder, indentManager);
            WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
            return snippetBuilder.ToString();
        }

        private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder payloadSb, IndentManager indentManager)
        {
            var responseAssignment = codeGraph.HasReturnedBody() ? "var result = " : string.Empty;
            var methodName = codeGraph.HttpMethod.Method.ToLower().ToFirstCharacterUpperCase() + "Async";
            var requestPayloadParameterName = codeGraph.HasBody() ? RequestBodyVarName : default;
            var requestConfigurationPayload = GetRequestConfiguration(codeGraph, indentManager);
            var parametersList = GetActionParametersList(requestPayloadParameterName , requestConfigurationPayload);
            payloadSb.AppendLine($"{responseAssignment}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{methodName}({parametersList});");
        }

        private static string GetRequestConfiguration(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasHeaders() && !snippetCodeGraph.HasParameters())
                return default;

            var requestConfigurationBuilder = new StringBuilder();
            requestConfigurationBuilder.AppendLine($"({RequestConfigurationVarName}) =>");
            requestConfigurationBuilder.AppendLine($"{indentManager.GetIndent()}{{");
            WriteRequestQueryParameters(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            WriteRequestHeaders(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            requestConfigurationBuilder.Append($"{indentManager.GetIndent()}}}");
            return requestConfigurationBuilder.ToString();
        }

        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
            return nonEmptyParameters.Any() ? string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}")) : string.Empty;
        }

        private static void WriteRequestHeaders(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder)
        {
            if (!snippetCodeGraph.HasHeaders()) 
                return ;

            indentManager.Indent();
            foreach (var header in snippetCodeGraph.Headers)
            {
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestHeadersPropertyName}.Add(\"{header.Name}\", \"{header.Value.EscapeQuotes()}\");");
            }
            indentManager.Unindent();
        }

        private static void WriteRequestQueryParameters(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder)
        {
            if (!snippetCodeGraph.HasParameters())
                return;
                
            indentManager.Indent();
            foreach(var queryParam in snippetCodeGraph.Parameters) 
            {
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestParametersPropertyName}.{queryParam.Name.ToLower().ToFirstCharacterUpperCase()} = {GetQueryParameterValue(queryParam)};");
            }
            indentManager.Unindent();
        }

        private static string GetQueryParameterValue(CodeProperty queryParam)
        {
            switch (queryParam.PropertyType)
            {
                case PropertyType.Boolean:
                    return queryParam.Value.ToLowerInvariant(); // Boolean types
                case PropertyType.Int32:
                case PropertyType.Int64:
                case PropertyType.Double:
                case PropertyType.Float32:
                case PropertyType.Float64:
                    return queryParam.Value; // Numbers stay as is 
                case PropertyType.Array:
                    return $"new string []{{ {string.Join(",", queryParam.Children.Select(x =>  $"\"{x.Value}\"" ).ToList())} }}"; // deconstruct arrays
                default:
                    return $"\"{queryParam.Value.EscapeQuotes()}\"";
            }
        }

        private static void WriteRequestPayloadAndVariableName(SnippetCodeGraph snippetCodeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasBody())
                return;// No body

            if(indentManager == null) 
                throw new ArgumentNullException(nameof(indentManager));

            switch (snippetCodeGraph.Body.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"var {RequestBodyVarName} = new {snippetCodeGraph.Body.Name.ToFirstCharacterUpperCase()}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    snippetCodeGraph.Body.Children.ForEach( child => WriteObjectFromCodeProperty(snippetCodeGraph.Body, child, snippetBuilder, indentManager));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}};");
                    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"using var {RequestBodyVarName} = new MemoryStream(); //stream to upload");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type for request: {snippetCodeGraph.Body.PropertyType}");
            }
        }

        private static void WriteObjectFromCodeProperty(CodeProperty parentProperty, CodeProperty codeProperty,StringBuilder snippetBuilder, IndentManager indentManager) 
        {
            indentManager.Indent();
            var isParentArray = parentProperty.PropertyType == PropertyType.Array;
            var isParentMap = parentProperty.PropertyType == PropertyType.Map;
            var assignmentSuffix = isParentMap ? string.Empty : ","; // no comma separator values for additionalData/maps
            var propertyAssignment = $"{indentManager.GetIndent()}{codeProperty.Name.CleanupSymbolName().ToFirstCharacterUpperCase()} = "; // default assignments to the usual "var x = xyz"
            if (isParentMap)
            {
                propertyAssignment = $"{indentManager.GetIndent()}\"{codeProperty.Name}\" , "; // if its in the additionalData assignments happen using string value keys
            }
            else if (isParentArray)
            {
                propertyAssignment = $"{indentManager.GetIndent()}"; // no assignments as entries as added directly to the collection/array
            }

            switch (codeProperty.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"{propertyAssignment}new {codeProperty.TypeDefinition.ToFirstCharacterUpperCase()}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    codeProperty.Children.ForEach( child => WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{assignmentSuffix}");
                    break;
                case PropertyType.Map:
                    snippetBuilder.AppendLine($"{propertyAssignment}new Dictionary<string, object>");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager);
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    break;
                case PropertyType.Array :
                    snippetBuilder.AppendLine($"{propertyAssignment}new List<{codeProperty.Children.FirstOrDefault().TypeDefinition.ToFirstCharacterUpperCase()}>");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager);
                    });
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{assignmentSuffix}");
                    break;
                case PropertyType.Guid:
                case PropertyType.String:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\"{assignmentSuffix}");
                    break;
                case PropertyType.Enum:
                    var segments = codeProperty.Value.Split('.');
                    snippetBuilder.AppendLine($"{propertyAssignment}{segments.First()}.{segments.Last().ToLower().ToFirstCharacterUpperCase()}{assignmentSuffix}");
                    break;
                case PropertyType.Date:
                    snippetBuilder.AppendLine($"{propertyAssignment}DateTimeOffset.Parse(\"{codeProperty.Value}\"){assignmentSuffix}");
                    break;
                case PropertyType.Base64Url:
                    snippetBuilder.AppendLine($"{propertyAssignment}Convert.FromBase64String(\"{codeProperty.Value.EscapeQuotes()}\"){assignmentSuffix}");
                    break;
                case PropertyType.Float32:
                case PropertyType.Float64:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}f{assignmentSuffix}");
                    break;
                case PropertyType.Int64:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}L{assignmentSuffix}");
                    break;
                case PropertyType.Double:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}d{assignmentSuffix}");
                    break;
                case PropertyType.Null:
                case PropertyType.Int32:
                case PropertyType.Boolean:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value.ToFirstCharacterLowerCase()}{assignmentSuffix}");
                    break;
                case PropertyType.Duration:
                    snippetBuilder.AppendLine($"{propertyAssignment}TimeSpan.Parse(\"{codeProperty.Value}\"){assignmentSuffix}");
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

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            if(!(nodes?.Any() ?? false)) 
                return string.Empty;

            return nodes.Select(x => {
                                        if(x.Segment.IsCollectionIndex())
                                            return x.Segment.Replace("{", "[\"").Replace("}", "\"]");
                                        if (x.Segment.IsFunction())
                                            return x.Segment.Split('.').Last().ToFirstCharacterUpperCase();
                                        return x.Segment.ToFirstCharacterUpperCase();
                                      })
                                .Aggregate((x, y) =>
                                {
                                    var dot = y.StartsWith("[") ? string.Empty : ".";
                                    return $"{x}{dot}{y}";
                                });
        }
    }
}
