using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeSnippetsReflection.OpenAPI;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string ClientVarName = "graphClient";
        private const string RequestBodyVarName = "requestBody";
        private const string RequestConfigurationVarName = "requestConfiguration";
        private const string RequestHeadersPropertyName = "Headers";
        private const string RequestParametersPropertyName = "QueryParameters";
        private const string DefaultNamespace = "Microsoft.Graph";
        private const string UntypedNamespace = "Microsoft.Kiota.Abstractions.Serialization";
        private const string VersionInformationString = "// Code snippets are only available for the latest version. Current version is 5.x";
        private const string InitializationInfoString = "// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=csharp";

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
            var usedNamespaces = new HashSet<string>();// list of used namespaces in generation
            var indentManager = new IndentManager();
            var codeGraph = new SnippetCodeGraph(snippetModel);
            var codeSnippetBuilder = new StringBuilder($"{VersionInformationString}{Environment.NewLine}{Environment.NewLine}");
            var requestPayloadAndVariableNameBuilder = WriteRequestPayloadAndVariableNameBuilder(codeGraph, indentManager, usedNamespaces);
            var requestExecutionPathBuilder = WriteRequestExecutionPathBuilder(codeGraph, indentManager,usedNamespaces);
            var dependenciesBuilder = WriteDependenciesBuilder(usedNamespaces.Where( static ns => !string.IsNullOrEmpty(ns)).ToArray());

            return codeSnippetBuilder.Append(dependenciesBuilder) // dependencies first
                .Append(requestPayloadAndVariableNameBuilder) // request body
                .AppendLine(InitializationInfoString)
                .Append(requestExecutionPathBuilder)// request executor
                .ToString();
        }

        private static StringBuilder WriteDependenciesBuilder(string [] usedNamespaces)
        {
            var dependenciesStringBuilder = new StringBuilder();
            if (usedNamespaces.Length != 0)
            {
                dependenciesStringBuilder.AppendLine("// Dependencies");
                foreach (var modelNamespace in usedNamespaces)
                {
                    dependenciesStringBuilder.AppendLine($"using {modelNamespace};");
                }
                dependenciesStringBuilder.AppendLine();
            }

            return dependenciesStringBuilder;
        }

        private static StringBuilder WriteRequestExecutionPathBuilder(SnippetCodeGraph codeGraph, IndentManager indentManager,HashSet<string> usedNamespaces)
        {
            var payloadSb = new StringBuilder();
            var responseAssignment = codeGraph.HasReturnedBody() ? "var result = " : string.Empty;
            var requestPayloadParameterName = codeGraph.HasBody() ? RequestBodyVarName : default;
            if (string.IsNullOrEmpty(requestPayloadParameterName) && ((codeGraph.RequestSchema?.Properties?.Any() ?? false) || (codeGraph.RequestSchema?.AllOf?.Any(schema => schema.Properties.Any()) ?? false)))
                requestPayloadParameterName = "null";// pass a null parameter if we have a request schema expected but there is not body provided

            string pathSegment = $"{ClientVarName}.{GetFluentApiPath(codeGraph.Nodes, codeGraph,usedNamespaces)}";
            var methodName = codeGraph.GetSchemaFunctionCallPrefix() + "Async";
            if(codeGraph.Parameters.Any( static property => property.Name.Equals("skiptoken",StringComparison.OrdinalIgnoreCase) ||
                                                            property.Name.Equals("deltatoken",StringComparison.OrdinalIgnoreCase)))
            {// its a delta query and needs the opaque url passed over.
                payloadSb.AppendLine("// Note: The URI string parameter used with WithUrl() can be retrieved using the OdataNextLink or OdataDeltaLink property from a response object");
                pathSegment = $"{pathSegment}.WithUrl(\"{codeGraph.RequestUrl}\")";
                codeGraph.Parameters = new List<CodeProperty>();// clear the query parameters as these will be provided in the url directly.
            }

            var requestConfigurationPayload = GetRequestConfiguration(codeGraph, indentManager);
            var parametersList = GetActionParametersList(requestPayloadParameterName , requestConfigurationPayload);
            payloadSb.AppendLine($"{responseAssignment}await {pathSegment}.{methodName}({parametersList});");
            return payloadSb;
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
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p)).ToArray();
            return nonEmptyParameters.Length != 0 ? string.Join(", ", nonEmptyParameters.Aggregate(static (a, b) => $"{a}, {b}")) : string.Empty;
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

        private static StringBuilder WriteRequestPayloadAndVariableNameBuilder(SnippetCodeGraph snippetCodeGraph, IndentManager indentManager, HashSet<string> usedNamespaces)
        {
            if (!snippetCodeGraph.HasBody())
                return new StringBuilder();// No body

            ArgumentNullException.ThrowIfNull(indentManager);

            var snippetBuilder = new StringBuilder();
            switch (snippetCodeGraph.Body.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"var {RequestBodyVarName} = new {GetTypeString(snippetCodeGraph.Body)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    usedNamespaces.Add(GetNamespaceName(snippetCodeGraph.Body.NamespaceName, snippetCodeGraph.ApiVersion));// add the namespace for the object for tracking
                    snippetCodeGraph.Body.Children.ForEach( child => WriteObjectFromCodeProperty(snippetCodeGraph.Body, child, snippetBuilder, indentManager,snippetCodeGraph.ApiVersion,usedNamespaces));
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}};");
                    break;
                case PropertyType.Binary:
                    snippetBuilder.AppendLine($"using var {RequestBodyVarName} = new MemoryStream(); //stream to upload");
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported property type for request: {snippetCodeGraph.Body.PropertyType}");
            }

            return snippetBuilder.AppendLine();
        }

        private static void WriteObjectFromCodeProperty(CodeProperty parentProperty, CodeProperty codeProperty,StringBuilder snippetBuilder, IndentManager indentManager, string apiVersion, HashSet<string> usedNamespaces)
        {
            indentManager.Indent();
            var isParentArray = parentProperty.PropertyType == PropertyType.Array;
            var isParentMap = parentProperty.PropertyType == PropertyType.Map;
            var assignmentSuffix = isParentMap ? string.Empty : ","; // no comma separator values for additionalData/maps
            var propertyAssignment = $"{indentManager.GetIndent()}{codeProperty.Name.CleanupSymbolName().ToPascalCase()} = "; // default assignments to the usual "var x = xyz"
            if (isParentMap)
            {
                propertyAssignment = $"{indentManager.GetIndent()}\"{codeProperty.Name}\" , "; // if its in the additionalData assignments happen using string value keys
            }
            else if (isParentArray)
            {
                propertyAssignment = $"{indentManager.GetIndent()}"; // no assignments as entries as added directly to the collection/array
            }

            usedNamespaces.Add(GetNamespaceName(codeProperty.NamespaceName, apiVersion));// add the namespace for the object for tracking
            if (codeProperty.TypeDefinition?.Equals(SnippetCodeGraph.UntypedNodeName, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                // write the untyped property
                WriteUntypedCodeProperty(codeProperty, snippetBuilder, indentManager, usedNamespaces, propertyAssignment);
                indentManager.Unindent();
                return; // nothing more to do here..
            }
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Object:
                    var typeString = GetTypeString(codeProperty);
                    if (string.IsNullOrEmpty(typeString))
                    {
                        // write the untyped property as we don't have a type associated with the "anonymous" object
                        var localObjectBuilder = new StringBuilder();
                        WriteUntypedCodeProperty(codeProperty, localObjectBuilder, indentManager, usedNamespaces, propertyAssignment);
                        snippetBuilder.AppendLine($"{localObjectBuilder.ToString().TrimEnd().TrimEnd(',')}{assignmentSuffix}");
                    }
                    else
                    {
                        snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty)}");
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                        codeProperty.Children.ForEach( child => WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion,usedNamespaces));
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}{assignmentSuffix}");
                    }
                    break;
                case PropertyType.Map:
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion,usedNamespaces);
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    break;
                case PropertyType.Array :
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty)}");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteObjectFromCodeProperty(codeProperty, child, snippetBuilder, indentManager,apiVersion,usedNamespaces);
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
                    var enumTypeString = GetTypeString(codeProperty);
                    var enumValues = codeProperty.Value.Split(new []{'|',','}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x =>
                        {
                            var enumHint = x.Split('.')[^1].Trim();
                            // the enum member may be invalid so default to generating the first value in case a look up fails.
                            var enumMember = codeProperty.Children.Find( member => member.Value.Equals(enumHint,StringComparison.OrdinalIgnoreCase)).Value ?? codeProperty.Children.FirstOrDefault().Value ?? enumHint;
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
                    snippetBuilder.AppendLine($"{propertyAssignment}new {GetTypeString(codeProperty)}(DateTime.Parse(\"{codeProperty.Value}\")){assignmentSuffix}");
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

        
        private static void WriteUntypedCodeProperty(CodeProperty codeProperty, StringBuilder snippetBuilder,
            IndentManager indentManager, HashSet<string> usedNamespaces, string propertyAssignment = "")
        {
            if (string.IsNullOrEmpty(propertyAssignment))
                propertyAssignment = indentManager.GetIndent();
            
            // ensure the namespace is included in the generated snippet.
            usedNamespaces.Add(UntypedNamespace);
            
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Object:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedObject(new Dictionary<string, UntypedNode>");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                        indentManager.Indent();
                        var localObjectBuilder = new StringBuilder();
                        WriteUntypedCodeProperty(child, localObjectBuilder, indentManager, usedNamespaces);
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}\"{child.Name}\", {localObjectBuilder.ToString().Trim().TrimEnd(',')}");
                        indentManager.Unindent();
                        snippetBuilder.AppendLine($"{indentManager.GetIndent()}}},");
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}),");
                    break;
                case PropertyType.Array:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedArray(new List<UntypedNode>");
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}{{");
                    indentManager.Indent();
                    codeProperty.Children.ForEach(child =>
                    {
                        WriteUntypedCodeProperty(child, snippetBuilder, indentManager, usedNamespaces);
                    });
                    indentManager.Unindent();
                    snippetBuilder.AppendLine($"{indentManager.GetIndent()}}}),");
                    break;
                case PropertyType.String:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedString(\"{codeProperty.Value}\"),");
                    break;
                case PropertyType.Boolean:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedBoolean({codeProperty.Value.ToLowerInvariant()}),");
                    break;
                case PropertyType.Null:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedNull(),");
                    break;
                case PropertyType.Double:
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedDouble({codeProperty.Value}),");
                    break;
                default: // default to string
                    snippetBuilder.AppendLine($"{propertyAssignment}new UntypedString(\"{codeProperty.Value}\"),");
                    break;
            }
        }

        private const string StringTypeName = "string";

        private static string GetTypeString(CodeProperty codeProperty)
        {
            var typeString = codeProperty.TypeDefinition.ToFirstCharacterUpperCase() ??
                             codeProperty.Value.ToFirstCharacterUpperCase();
            switch (codeProperty.PropertyType)
            {
                case PropertyType.Array:
                    // For objects, rely on the typeDefinition from the array definition otherwise look deeper for primitive collections
                    var collectionTypeString = codeProperty.Children.Count != 0 && codeProperty.Children[0].PropertyType != PropertyType.Object
                        ? GetTypeString(codeProperty.Children[0])
                        : ReplaceIfReservedTypeName(typeString);
                    if(string.IsNullOrEmpty(collectionTypeString))
                        collectionTypeString = "object";
                    else if(typeString.Equals(StringTypeName,StringComparison.OrdinalIgnoreCase))
                        collectionTypeString = StringTypeName; // use the conventional casing if need be
                    return $"List<{collectionTypeString}>";
                case PropertyType.Object:
                    return $"{ReplaceIfReservedTypeName(typeString)}";
                case PropertyType.Map:
                    return "Dictionary<string, object>";
                case PropertyType.String:
                    return StringTypeName;
                case PropertyType.Enum:
                    return $"{ReplaceIfReservedTypeName(typeString.Split('.')[0])}?";
                case PropertyType.DateOnly:
                    return "Date";
                case PropertyType.TimeOnly:
                    return "Time";
                case PropertyType.Guid:
                    return "Guid?";
                default:
                    return ReplaceIfReservedTypeName(typeString);
            }
        }

        private static string GetNamespaceName(string namespaceName, string apiVersion)
        {
            if (string.IsNullOrEmpty(namespaceName))
                return string.Empty;

            //strip the default namespace name from the original as the models are typically already in Microsoft.Graph namespace
            namespaceName = namespaceName.Replace(DefaultNamespace,string.Empty, StringComparison.OrdinalIgnoreCase);

            var normalizedNameSpaceName = namespaceName.TrimStart('.').Split('.',StringSplitOptions.RemoveEmptyEntries)
                .Select(static x => ReplaceIfReservedTypeName(x, "Namespace").ToFirstCharacterUpperCase())
                .Aggregate(static (z, y) => z + '.' + y);

            return $"{GetDefaultNamespaceName(apiVersion)}.{normalizedNameSpaceName}";
        }

        private static string GetDefaultNamespaceName(string apiVersion) =>
            apiVersion.Equals("beta", StringComparison.OrdinalIgnoreCase) ?  $"{DefaultNamespace}.Beta" : DefaultNamespace;

        private static string ReplaceIfReservedTypeName(string originalString, string suffix = "Object")
            => ReservedNames.Value.Contains(originalString) ? $"{originalString}{suffix}" : originalString;

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes, SnippetCodeGraph snippetCodeGraph, HashSet<string> usedNamespaces)
        {
            if(!(nodes?.Any() ?? false))
                return string.Empty;

            return nodes.Select(x => {
                                        if (x.Segment.IsCollectionIndex())
                                        {
                                            //Handle cases where the indexer is an integer type
                                            var isIntParam = false;
                                            if (x.PathItems.TryGetValue("default", out var pathItem))
                                            {
                                                isIntParam = pathItem.Parameters.Any(parameter => x.Segment.Contains(parameter.Name) && (parameter.Schema.Type.Equals("integer", StringComparison.OrdinalIgnoreCase) 
                                                                                                                                            || parameter.Schema.Type.Equals("number", StringComparison.OrdinalIgnoreCase)));
                                            }
                                            return isIntParam ? "[2]" : x.Segment.Replace("{", "[\"{").Replace("}", "}\"]");
                                        }
                                        if (x.Segment.IsFunctionWithParameters())
                                        {
                                            var functionName = x.Segment.Split('(')[0];
                                            functionName = functionName.Split(".",StringSplitOptions.RemoveEmptyEntries)
                                                                        .Select(static s => s.ToFirstCharacterUpperCase())
                                                                        .Aggregate(static (a, b) => $"{a}{b}");
                                            var parameters = snippetCodeGraph.AggregatePathParametersIntoString();

                                            // use the existing WriteObjectFromCodeProperty functionality to write the parameters as if they were a comma seperated array so as to automatically infer type handling from the codeDom :)
                                            var parametersBuilder = new StringBuilder();
                                            foreach (var codeProperty in snippetCodeGraph.PathParameters.OrderBy(static parameter => parameter.Name, StringComparer.OrdinalIgnoreCase))
                                            {
                                                var parameter = new StringBuilder();
                                                WriteObjectFromCodeProperty(new CodeProperty{PropertyType = PropertyType.Array}, codeProperty, parameter, new IndentManager(), snippetCodeGraph.ApiVersion, usedNamespaces);
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
                                    var dot = y.StartsWith('[') ? string.Empty : ".";
                                    return $"{x}{dot}{y}";
                                });
        }
    }
}
