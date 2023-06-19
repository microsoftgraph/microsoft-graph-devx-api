using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string ModelNamespacePrefixToTrim = $"Models.{DefaultNamespace}";
        private const string DefaultNamespace = "Microsoft.Graph";
        
        private static HashSet<string> GetSystemTypeNames()
        {
            return typeof(string).Assembly.GetTypes()
                .Where(static type => type.Namespace == "System" 
                                      && type.IsPublic // get public(we can only import public type in external code)
                                      && !type.IsGenericType)// non generic types(generic type names have special character like `)
                .Select(static type => type.Name)
                .ToHashSet();
        }
        
        private static readonly HashSet<string> CustomDefinedValues = new(StringComparer.OrdinalIgnoreCase)
        {
            "file", //system.io static types
            "directory",
            "path",
            "environment",
            "task",
            "thread",
            "integer"
        };
        
        private static readonly Lazy<HashSet<string>> ReservedNames = new(static () =>
        {
            CustomDefinedValues.UnionWith(GetSystemTypeNames());
            return CustomDefinedValues;
        });
        
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder($"// Code snippets are only available for the latest version. Current version is 5.x{Environment.NewLine}{Environment.NewLine}" +
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
            if (string.IsNullOrEmpty(requestPayloadParameterName) && ((codeGraph.RequestSchema?.Properties?.Any() ?? false) || (codeGraph.RequestSchema?.AllOf?.Any(schema => schema.Properties.Any()) ?? false)))
                requestPayloadParameterName = "null";// pass a null parameter if we have a request schema expected but there is not body provided
            var requestConfigurationPayload = GetRequestConfiguration(codeGraph, indentManager);
            var parametersList = GetActionParametersList(requestPayloadParameterName , requestConfigurationPayload);
            payloadSb.AppendLine($"{responseAssignment}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes,codeGraph)}.{methodName}({parametersList});");
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
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
            return nonEmptyParameters.Any() ? string.Join(", ", nonEmptyParameters.Aggregate(static (a, b) => $"{a}, {b}")) : string.Empty;
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
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestParametersPropertyName}.{queryParam.Name.ToFirstCharacterUpperCase()} = {GetQueryParameterValue(queryParam)};");
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
                    return $"new string []{{ {string.Join(",", queryParam.Children.Select(static x =>  $"\"{x.Value}\"" ).ToList())} }}"; // deconstruct arrays
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
                    snippetBuilder.AppendLine($"var {RequestBodyVarName} = new {GetTypeString(snippetCodeGraph.Body, snippetCodeGraph.ApiVersion)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    snippetCodeGraph.Body.Children.ForEach( child => WriteObjectFromCodeProperty(snippetCodeGraph.Body, child, snippetBuilder, indentManager,snippetCodeGraph.ApiVersion));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}};");
                    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"using var {RequestBodyVarName} = new MemoryStream(); //stream to upload");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type for request: {snippetCodeGraph.Body.PropertyType}");
            }
        }

        private static void WriteObjectFromCodeProperty(CodeProperty parentProperty, CodeProperty codeProperty,StringBuilder snippetBuilder, IndentManager indentManager, string apiVersion) 
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
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty,apiVersion)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    codeProperty.Children.ForEach( child => WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{assignmentSuffix}");
                    break;
                case PropertyType.Map:
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty,apiVersion)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion);
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    break;
                case PropertyType.Array :
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty, apiVersion)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion);
                    });
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{assignmentSuffix}");
                    break;
                case PropertyType.Guid:
                    snippetBuilder.AppendLine($"{propertyAssignment}Guid.Parse(\"{codeProperty.Value}\"){assignmentSuffix}");
                    break;
                case PropertyType.String:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\"{assignmentSuffix}");
                    break;
                case PropertyType.Enum:
                    var enumTypeString = GetTypeString(codeProperty, apiVersion);
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
                case PropertyType.DateTime:
                    snippetBuilder.AppendLine($"{propertyAssignment}DateTimeOffset.Parse(\"{codeProperty.Value}\"){assignmentSuffix}");
                    break;
                case PropertyType.DateOnly:
                case PropertyType.TimeOnly:
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty, apiVersion)}(DateTime.Parse(\"{codeProperty.Value}\")){assignmentSuffix}");
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


        private static string GetTypeString(CodeProperty codeProperty, string apiVersion)
        {
            var typeString = codeProperty.TypeDefinition.ToFirstCharacterUpperCase() ??
                             codeProperty.Value.ToFirstCharacterUpperCase();
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Array:
                    var collectionTypeString = codeProperty.Children.Any()
                        ? GetTypeString(codeProperty.Children.First(), apiVersion)
                        : typeString;
                    return $"List<{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{collectionTypeString}>";
                case PropertyType.Object:
                    return $"{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{ReplaceIfReservedTypeName(typeString)}";
                case PropertyType.Map:
                    return "Dictionary<string, object>";
                case PropertyType.String:
                    return "string";
                case PropertyType.Enum:
                    return $"{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{ReplaceIfReservedTypeName(typeString.Split('.').First())}?";
                case PropertyType.DateOnly:
                    return "Date";
                case PropertyType.TimeOnly:
                    return "Time";
                default:
                    return ReplaceIfReservedTypeName(typeString);
            }
        }

        private static string GetNamespaceName(string namespaceName, string apiVersion)
        {
            if (string.IsNullOrEmpty(namespaceName) || namespaceName.Equals(ModelNamespacePrefixToTrim, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            //strip the default namespace name from the original as the models are typically already in Microsoft.Graph namespace
            namespaceName = namespaceName.Replace(DefaultNamespace,string.Empty, StringComparison.OrdinalIgnoreCase);
            
            var normalizedNameSpaceName = namespaceName.TrimStart('.').Split('.',StringSplitOptions.RemoveEmptyEntries)
                .Select(static x => ReplaceIfReservedTypeName(x, "Namespace").ToFirstCharacterUpperCase())
                .Aggregate(static (z, y) => z + '.' + y);

            return $"{GetDefaultNamespaceName(apiVersion)}.{normalizedNameSpaceName}.";
        }

        private static string GetDefaultNamespaceName(string apiVersion) =>
            apiVersion.Equals("beta", StringComparison.OrdinalIgnoreCase) ?  $"{DefaultNamespace}.Beta" : DefaultNamespace;
        
        private static string ReplaceIfReservedTypeName(string originalString, string suffix = "Object")
            => ReservedNames.Value.Contains(originalString) ? $"{originalString}{suffix}" : originalString;

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes, SnippetCodeGraph snippetCodeGraph)
        {
            if(!(nodes?.Any() ?? false)) 
                return string.Empty;

            return nodes.Select(x => {
                                        if(x.Segment.IsCollectionIndex())
                                            return x.Segment.Replace("{", "[\"{").Replace("}", "}\"]");
                                        if (x.Segment.IsFunctionWithParameters())
                                        {
                                            var functionName = x.Segment.Split('(').First();
                                            functionName = functionName.Split(".",StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(static s => s.ToFirstCharacterUpperCase())
                                                                        .Aggregate(static (a, b) => $"{a}{b}");
                                            var parameters = snippetCodeGraph.PathParameters
                                                .Select(static s => $"With{s.Name.ToFirstCharacterUpperCase()}")
                                                .Aggregate(static (a, b) => $"{a}{b}");

                                            // use the existing WriteObjectFromCodeProperty functionality to write the parameters as if they were a comma seperated array so as to automatically infer type handling from the codeDom :)
                                            var parametersBuilder = new StringBuilder();
                                            foreach (var codeProperty in snippetCodeGraph.PathParameters.OrderBy(static parameter => parameter.Name, StringComparer.OrdinalIgnoreCase))
                                            {
                                                var parameter = new StringBuilder();
                                                WriteObjectFromCodeProperty(new CodeProperty{PropertyType = PropertyType.Array}, codeProperty, parameter, new IndentManager(), snippetCodeGraph.ApiVersion);
                                                parametersBuilder.Append(parameter.ToString().Trim());//Do this to trim the surrounding whitespace generated
                                            }
                                            
                                            return functionName.ToFirstCharacterUpperCase()
                                                   + parameters
                                                   + $"({parametersBuilder.ToString().TrimEnd(',')})" ;
                                        }
                                        if (x.Segment.IsFunction())
                                            return x.Segment.RemoveFunctionBraces().Split('.')
                                                .Select(static s => s.ToFirstCharacterUpperCase())
                                                .Aggregate(static (a, b) => $"{a}{b}");
                                        return x.Segment.ReplaceValueIdentifier().TrimStart('$').RemoveFunctionBraces().ToFirstCharacterUpperCase();
                                      })
                                .Aggregate(static (x, y) =>
                                {
                                    var dot = y.StartsWith("[") ? string.Empty : ".";
                                    return $"{x}{dot}{y}";
                                });
        }
    }
}
