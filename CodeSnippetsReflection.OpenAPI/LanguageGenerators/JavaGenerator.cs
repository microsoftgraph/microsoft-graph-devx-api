﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class JavaGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        // Constants for the code snippet
        private const string ClientVarName = "graphClient";
        private const string ClientVarType = "GraphServiceClient";
        private const string HttpCoreVarName = "requestAdapter";
        private const string RequestConfigurationVarName = "requestConfiguration";
        private const string RequestHeadersPropertyName = "headers";
        private const string RequestParametersPropertyName = "queryParameters";

        // Constants for reading from the OpenAPI document
        private const string ModelNamespacePrefixToTrim = $"models.{DefaultNamespace}";
        private const string DefaultNamespace = "microsoft.graph";

        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "abstract", 
            "assert",
            "boolean",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "class",
            "const",
            "continue",
            "default",
            "double",
            "do",
            "else",
            "enum",
            "extends",
            "false", 
            "final",
            "finally",
            "float",
            "for",
            "goto",
            "if",
            "implements",
            "import",
            "instanceof",
            "int",
            "interface",
            "long",
            "native",
            "new",
            "null",
            "package",
            "private",
            "protected",
            "public",
            "return",
            "short",
            "static",
            "strictfp",
            "super",
            "switch",
            "synchronized",
            "this",
            "throw",
            "throws",
            "transient",
            "true",
            "try",
            "void",
            "volatile",
            "while"
        };

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder($"// Code snippets are only available for the latest version. Current version is 6.x{Environment.NewLine}{Environment.NewLine}" +
                                                   $"{ClientVarType} {ClientVarName} = new {ClientVarType}({HttpCoreVarName});{Environment.NewLine}{Environment.NewLine}");

            WriteRequestPayloadAndVariableName(codeGraph, snippetBuilder, indentManager);
            WriteRequestExecutionPath(codeGraph, snippetBuilder, indentManager);
            return snippetBuilder.ToString();
        }    

        private static void WriteRequestExecutionPath(SnippetCodeGraph codeGraph, StringBuilder payloadSb, IndentManager indentManager)
        {
            var responseAssignment = codeGraph.HasReturnedBody() ? $"{codeGraph.ResponseSchema.Reference?.Id.Replace($"{DefaultNamespace}.", string.Empty).ToPascalCase() ?? "var"} result = " : string.Empty;
            var methodName = codeGraph.HttpMethod.Method.ToLower();
            var requestPayloadParameterName = codeGraph.HasBody() ? codeGraph.Body.PropertyType == PropertyType.Binary ? "stream" : GetPropertyObjectName(codeGraph.Body).ToFirstCharacterLowerCase() : default;

            if (string.IsNullOrEmpty(requestPayloadParameterName) && ((codeGraph.RequestSchema?.Properties?.Any() ?? false) || (codeGraph.RequestSchema?.AllOf?.Any(schema => schema.Properties.Any()) ?? false)))
                requestPayloadParameterName = "null";// pass a null parameter if we have a request schema expected but there is not body provided

            string pathSegment;
            if(codeGraph.Nodes.Last().Segment.Contains("delta") && 
               codeGraph.Parameters.Any( static property => property.Name.Equals("skiptoken",StringComparison.OrdinalIgnoreCase) || 
                                                            property.Name.Equals("deltatoken",StringComparison.OrdinalIgnoreCase)))
            {// its a delta query and needs the opaque url passed over.
                pathSegment = "deltaRequestBuilder.";
                codeGraph.Parameters = new List<CodeProperty>();// clear the query parameters as these will be provided in the url directly.
                payloadSb.AppendLine($"DeltaRequestBuilder deltaRequestBuilder = new com.{GetDefaultNamespaceName(codeGraph.ApiVersion)}.{GetFluentApiPath(codeGraph.Nodes, codeGraph).Replace("()","")}DeltaRequestBuilder(\"{codeGraph.RequestUrl}\", {ClientVarName}.getRequestAdapter()).");
                responseAssignment = "DeltaResponse result = ";
            }
            else
            {
                pathSegment = $"{ClientVarName}.{GetFluentApiPath(codeGraph.Nodes, codeGraph)}";
            }
            
            var requestConfigurationPayload = GetRequestConfiguration(codeGraph, indentManager);
            var parametersList = GetActionParametersList(requestPayloadParameterName , requestConfigurationPayload);
            payloadSb.AppendLine($"{responseAssignment}{pathSegment}{methodName}({parametersList}).get();");
        }



        private static string GetRequestConfiguration(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasHeaders() && !snippetCodeGraph.HasParameters())
                return default;

            var requestConfigurationBuilder = new StringBuilder();
            requestConfigurationBuilder.AppendLine($"{RequestConfigurationVarName} -> {{");
            WriteRequestQueryParameters(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            WriteRequestHeaders(snippetCodeGraph, indentManager, requestConfigurationBuilder);
            requestConfigurationBuilder.Append($"}}");
            return requestConfigurationBuilder.ToString();
        }

        private static void WriteRequestQueryParameters(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder)
        {
            if (!snippetCodeGraph.HasParameters())
                return;

            indentManager.Indent();
            foreach (var queryParam in snippetCodeGraph.Parameters)
            {
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestParametersPropertyName}.{queryParam.Name.ToFirstCharacterLowerCase()} = {GetQueryParameterValue(queryParam)};");
            }
            indentManager.Unindent();
        }

        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
            return nonEmptyParameters.Any() ? string.Join(", ", nonEmptyParameters.Aggregate(static (a, b) => $"{a}, {b}")) : string.Empty;
        }

        private static void WriteRequestHeaders(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, StringBuilder stringBuilder)
        {
            if (!snippetCodeGraph.HasHeaders())
                return;

            indentManager.Indent();
            foreach (var header in snippetCodeGraph.Headers)
            {
                stringBuilder.AppendLine($"{indentManager.GetIndent()}{RequestConfigurationVarName}.{RequestHeadersPropertyName}.add(\"{header.Name}\", \"{header.Value.EscapeQuotes()}\");");
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
                    return $"new String []{{{string.Join(", ", queryParam.Children.Select(static x => $"\"{x.Value}\"").ToList())}}}"; // deconstruct arrays
                default:
                    return $"\"{queryParam.Value.EscapeQuotes()}\"";
            }
        }


        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes, SnippetCodeGraph codeGraph)
        {
            if (!(nodes?.Any() ?? false)) return string.Empty;
            var elements = nodes.Select(x =>
            {
                if (x.Segment.IsCollectionIndex())
                {
                    var pathName = string.IsNullOrEmpty(x.Segment) ? x.Segment : x.Segment.ReplaceMultiple("", "{", "}")
                                                                                          .Split('-')
                                                                                          .Where(static s => !string.IsNullOrEmpty(s))
                                                                                          .Select(static s => s.ToFirstCharacterUpperCase())
                                                                                          .Aggregate(static (a, b) => $"By{a}{b}");
                    return $"{pathName ?? "ByTypeId"}{x.Segment.Replace("{", "(\"{", StringComparison.OrdinalIgnoreCase).Replace("}", "}\")", StringComparison.OrdinalIgnoreCase)}.";
                }
                if (x.Segment.IsFunctionWithParameters())
                {
                    //var functionName = x.Segment.Split('(').First();
                    var functionName = x.Segment.Split('(').First()
                                                .Split(".", StringSplitOptions.RemoveEmptyEntries)
                                                .Select(static s => s.ToPascalCase())
                                                .Aggregate(static (a, b) => $"{a}{b}");

                    var parameters = codeGraph.PathParameters
                                                .Select(static s => $"With{s.Name.ToPascalCase()}")
                                                .Aggregate(static (a, b) => $"{a}{b}");

                    string parameterDeclarations = string.Join(", ", codeGraph.PathParameters
                                                            .OrderBy(static parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
                                                            .Select(parameter => GetPathParameterDeclaration(parameter, codeGraph.ApiVersion)));
                    return $"{functionName.ToFirstCharacterLowerCase()}{parameters}({parameterDeclarations}).";
                }
                if (x.Segment.IsFunction())
                {
                    return x.Segment.Split('.')
                              .Select(static s => s.ToFirstCharacterLowerCase())
                              .Aggregate(static (a, b) => $"{a}{b}").ToFirstCharacterUpperCase()+"().";
                }
                return x.Segment.ToFirstCharacterLowerCase()+"().";
            })
                        .Aggregate(new List<string>(), static (current, next) =>
                        {
                            var element = next.Contains("ByTypeId", StringComparison.OrdinalIgnoreCase) ?
                            next.Replace("ByTypeId", $"By{current.Last().Replace("s().", string.Empty, StringComparison.OrdinalIgnoreCase)}Id") :
                            $"{next.Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase).ToFirstCharacterLowerCase()}";

                            current.Add(element);
                            return current;
                        });

            return string.Join("", elements).Replace("()()", "()").Replace("..",".");
        }

        private static string GetPathParameterDeclaration(CodeProperty pathParameter, string apiVersion)
        {
            switch (pathParameter.PropertyType)
            {
                case PropertyType.String:
                    return $"\"{pathParameter.Value}\"";
                case PropertyType.Null:
                case PropertyType.Boolean:
                    return pathParameter.Value.ToLowerInvariant();
                case PropertyType.Enum:
                    return pathParameter.Value.ToFirstCharacterUpperCase();
                case PropertyType.DateTime:
                case PropertyType.DateOnly:
                case PropertyType.TimeOnly:
                    return $"{GetTypeString(pathParameter, apiVersion)}.parse(\"{pathParameter.Value}\")";
                case PropertyType.Duration:
                    return $"PeriodAndDuration.ofDuration(Duration.Parse(\"{pathParameter.Value}\"))";
                case PropertyType.Float32:
                case PropertyType.Float64:
                    return $"{pathParameter.Value}f";
                case PropertyType.Int64:
                    return $"{pathParameter.Value}L";
                case PropertyType.Double:
                    return $"{pathParameter.Value}d";
                default:
                    return pathParameter.Value;
            }
        }

        private static string ReplaceIfReservedName(string originalString, string suffix = "Object")
            => ReservedNames.Contains(originalString) ? $"{originalString}{suffix}" : originalString;

        private static void WriteRequestPayloadAndVariableName(SnippetCodeGraph snippetCodeGraph, StringBuilder snippetBuilder, IndentManager indentManager)
        {
            if (!snippetCodeGraph.HasBody())
                return;// No body

            if (indentManager == null)
                throw new ArgumentNullException(nameof(indentManager));

            switch (snippetCodeGraph.Body.PropertyType)
            {
                case PropertyType.Object:
                    var typeString = GetTypeString(snippetCodeGraph.Body, snippetCodeGraph.ApiVersion);
                    snippetBuilder.AppendLine($"{typeString} {GetPropertyObjectName(snippetCodeGraph.Body).ToFirstCharacterLowerCase()} = new {typeString}();");
                    snippetCodeGraph.Body.Children.ForEach( child => WriteObjectFromCodeProperty(snippetCodeGraph.Body, child, snippetBuilder, indentManager,snippetCodeGraph.ApiVersion));
                    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"ByteArrayInputStream stream = new ByteArrayInputStream(new byte[0]); //stream to upload"); 
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type for request: {snippetCodeGraph.Body.PropertyType}");
            }
        }

        private static void WriteObjectFromCodeProperty(CodeProperty parentProperty, CodeProperty codeProperty, StringBuilder snippetBuilder, IndentManager indentManager, string apiVersion, string parentEnumerableValue = "", string enumerableValue = "")
        {
            var setterPrefix = "set";
            var typeString = GetTypeString(codeProperty, apiVersion);
            var currentPropertyName = $"{GetPropertyObjectName(codeProperty) ?? "property"}{enumerableValue}";
            var parentPropertyName = $"{GetPropertyObjectName(parentProperty) ?? "parentProperty"}{parentEnumerableValue}";
            
            var isParentArray = parentProperty.PropertyType == PropertyType.Array;
            var isParentMap = parentProperty.PropertyType == PropertyType.Map;
            
            var propertyAssignment = $"{parentPropertyName.ToFirstCharacterLowerCase()}.{setterPrefix}{GetPropertyObjectName(codeProperty)}("; // default assignments to the usual "primitive x = setX();"
            propertyAssignment = isParentArray ? $"{parentPropertyName.ToFirstCharacterLowerCase()}.add(" : propertyAssignment;
            propertyAssignment = isParentMap ? $"{parentPropertyName.ToFirstCharacterLowerCase()}.put(\"{codeProperty.Name}\", " : propertyAssignment;

            var assignment = $"{propertyAssignment}{currentPropertyName.ToFirstCharacterLowerCase() ?? "property"});";

            parentEnumerableValue = enumerableValue == "" ? string.Empty : enumerableValue; 


            switch (codeProperty.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"{typeString} {currentPropertyName.ToFirstCharacterLowerCase()} = new {typeString}();");
                    codeProperty.Children.ForEach(child => WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager, apiVersion, parentEnumerableValue, enumerableValue));
                    snippetBuilder.AppendLine(assignment);
                    break;
                case PropertyType.Array:
                case PropertyType.Map:
                    snippetBuilder.AppendLine($"{typeString} {currentPropertyName.ToFirstCharacterLowerCase()} = new {typeString}();");
                    var childEnumerationValue = 0;
                    codeProperty.Children.ForEach(child => {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager, apiVersion, parentEnumerableValue, $"{childEnumerationValue}");
                        childEnumerationValue++;
                    });
                    snippetBuilder.AppendLine(assignment);
                    break;
                case PropertyType.Guid:
                    snippetBuilder.AppendLine($"{propertyAssignment}UUID.fromString(\"{codeProperty.Value}\"));");
                    break;
                case PropertyType.Enum:
                    var enumTypeString = GetTypeString(codeProperty, apiVersion);
                    var enumValues = codeProperty.Value.Split(new []{'|',','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            var enumHint = x.Split('.').Last().Trim();
                            // the enum member may be invalid so default to generating the first value in case a look up fails.
                            var enumMember = codeProperty.Children.FirstOrDefault( member => member.Value.Equals(enumHint,StringComparison.OrdinalIgnoreCase)).Value ?? codeProperty.Children.FirstOrDefault().Value ?? enumHint;
                            return $"{enumTypeString}.{enumMember.ToFirstCharacterUpperCase()}";
                        })
                        .Aggregate(static (x, y) => $"{x} | {y}");
                    snippetBuilder.AppendLine($"{propertyAssignment}{enumValues});");
                    break;
                case PropertyType.DateTime:
                case PropertyType.DateOnly:
                case PropertyType.TimeOnly:
                    snippetBuilder.AppendLine($"{typeString} {currentPropertyName.ToFirstCharacterLowerCase()} = {typeString}.parse(\"{codeProperty.Value}\");");
                    snippetBuilder.AppendLine(assignment);
                    break;
                case PropertyType.Base64Url:
                    snippetBuilder.AppendLine($"byte[] {currentPropertyName.ToFirstCharacterLowerCase()} = Base64.getDecoder().decode(\"{codeProperty.Value.EscapeQuotes()}\");");
                    snippetBuilder.AppendLine(assignment);
                    break;
                case PropertyType.Float32:
                case PropertyType.Float64:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}f);");
                    break;
                case PropertyType.Int64:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}L);");
                    break;
                case PropertyType.Double:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value}d);");
                    break;
                case PropertyType.Null:
                case PropertyType.Int32:
                case PropertyType.Boolean:
                    snippetBuilder.AppendLine($"{propertyAssignment}{codeProperty.Value.ToFirstCharacterLowerCase()});");
                    break;
                case PropertyType.Duration:
                    snippetBuilder.AppendLine($"{typeString} {currentPropertyName.ToFirstCharacterLowerCase()} = PeriodAndDuration.ofDuration(Duration.Parse(\"{codeProperty.Value}\"));");
                    snippetBuilder.AppendLine(assignment);
                    break;
                case PropertyType.Binary:
                case PropertyType.Default:
                case PropertyType.String:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\");");
                    break;
                default:
                    snippetBuilder.AppendLine($"{propertyAssignment}\"{codeProperty.Value}\");");
                    break;
            }
        }

        private const string StringTypeName = "String";

        private static string GetTypeString(CodeProperty codeProperty, string apiVersion)
        {
            var typeString = codeProperty.TypeDefinition.ToFirstCharacterUpperCase() ??
                             codeProperty.Value.ToFirstCharacterUpperCase();
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Array:
                    // For objects, rely on the typeDefinition from the array definition otherwise look deeper for primitive collections
                    var collectionTypeString = codeProperty.Children.Any() && codeProperty.Children[0].PropertyType != PropertyType.Object
                        ? GetTypeString(codeProperty.Children[0], apiVersion)
                        : ReplaceIfReservedName(typeString);
                    if(string.IsNullOrEmpty(collectionTypeString)) 
                        collectionTypeString = "Object";
                    else if(typeString.Equals(StringTypeName,StringComparison.OrdinalIgnoreCase)) 
                        collectionTypeString = StringTypeName; // use the conventional casing if need be
                    return $"LinkedList<{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{collectionTypeString}>";
                case PropertyType.Object:
                    return $"{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{ReplaceIfReservedName(typeString)}";
                case PropertyType.Map:
                    return "HashMap<String, Object>";
                case PropertyType.String:
                    return StringTypeName;
                case PropertyType.Enum:
                    return $"{GetNamespaceName(codeProperty.NamespaceName,apiVersion)}{ReplaceIfReservedName(typeString.Split('.').First())}";
                case PropertyType.DateOnly:
                    return "LocalDate";
                case PropertyType.TimeOnly:
                    return "LocalTime";
                case PropertyType.Guid:
                    return "UUID";
                case PropertyType.DateTime:
                    return "OffsetDateTime";
                case PropertyType.Duration:
                    return "PeriodAndDuration";
                case PropertyType.Binary:
                    return "ByteArrayInputStream";
                default:
                    return ReplaceIfReservedName(typeString);
            }
        }

        private static string GetPropertyObjectName(CodeProperty codeProperty)
        {
            var propertyName = codeProperty.Name.CleanupSymbolName().ToPascalCase();
            return ReplaceIfReservedName(propertyName);
        }

        private static string GetNamespaceName(string namespaceName, string apiVersion)
        {
            if (string.IsNullOrEmpty(namespaceName) || namespaceName.Equals(ModelNamespacePrefixToTrim, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            //strip the default namespace name from the original as the models are typically already in Microsoft.Graph namespace
            namespaceName = namespaceName.Replace(DefaultNamespace,string.Empty, StringComparison.OrdinalIgnoreCase);
            
            var normalizedNameSpaceName = namespaceName.TrimStart('.').Split('.',StringSplitOptions.RemoveEmptyEntries)
                .Select(static x => ReplaceIfReservedName(x, "Namespace").ToLowerInvariant())
                .Aggregate(static (z, y) => z + '.' + y);

            return $"com.{GetDefaultNamespaceName(apiVersion)}.{normalizedNameSpaceName}.";
        }

        private static string GetDefaultNamespaceName(string apiVersion) =>
            apiVersion.Equals("beta", StringComparison.OrdinalIgnoreCase) ?  $"{DefaultNamespace}.beta" : DefaultNamespace;
    
    }
}