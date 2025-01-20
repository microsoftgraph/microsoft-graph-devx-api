using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class GoGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string clientVarName = "graphClient";
        private const string requestBodyVarName = "requestBody";
        private const string requestHeadersVarName = "headers";
        private const string optionsParameterVarName = "options";
        private const string requestOptionsVarName = "options";
        private const string requestParametersVarName = "requestParameters";
        private const string requestConfigurationVarName = "configuration";

        private static IImmutableSet<string> specialProperties = ImmutableHashSet.Create("@odata.type");

        private static IImmutableSet<string> NativeTypes = GetNativeTypes();

        private static readonly Regex PropertyNameRegex = new Regex(@"@(.*)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        private static readonly Regex FunctionRegex = new Regex(@"(\w+)\(([^)]*)\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        private static readonly Regex ParamRegex = new Regex(@"(\w+)\s*=\s*'[^']*'", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        static IImmutableSet<string> GetNativeTypes()
        {
            return ImmutableHashSet.Create("string", "int", "float");
        }

        private static readonly Dictionary<string, string> formatPropertyName = new(StringComparer.OrdinalIgnoreCase)
        {
            {"guid", "uuid.UUID"},
            {"uuid", "uuid.UUID"},
            {"date-time", "time.Time"},
            {"date", "serialization.DateOnly"},
            {"duration", "serialization.ISODuration"}
        };

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            ArgumentNullException.ThrowIfNull(snippetModel);

            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder();
            snippetBuilder.AppendLine("");

            var latestMajorVersion = "v1.0".Equals(codeGraph.ApiVersion) ? "v1.*" : "v0.*";
            snippetBuilder.AppendLine($"// Code snippets are only available for the latest major version. Current major version is ${latestMajorVersion}");
            snippetBuilder.AppendLine("");

            writeImportStatements(codeGraph, snippetBuilder);
            writeSnippet(codeGraph, snippetBuilder);

            return snippetBuilder.ToString();
        }

        private static void writeImportStatements(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            builder.AppendLine("// Dependencies");
            var apiVersion = "v1.0".Equals(codeGraph.ApiVersion) ? "msgraph-sdk-go" : "msgraph-beta-sdk-go";
            builder.AppendLine("import (");
            builder.AppendLine("\t  \"context\""); // default


            if (hasPropertyOfType(codeGraph, PropertyType.DateTime))
                builder.AppendLine("\t  \"time\""); // conditional time import

            if (hasPropertyOfType(codeGraph, PropertyType.Guid))
                builder.AppendLine("\t  \"github.com/google/uuid\""); // conditional uuid import

            if (codeGraph.HasHeaders() || hasPropertyOfType(codeGraph, PropertyType.Duration))
                builder.AppendLine($"\t  abstractions \"github.com/microsoft/kiota-abstractions-go\""); // conditional abstractions import

            builder.AppendLine($"\t  msgraphsdk \"github.com/microsoftgraph/{apiVersion}\""); // api version


            // add models
            var models = getModelsPaths(codeGraph);
            foreach (var path in models)
            {
                builder.AppendLine($"\t  graph{path.Replace(".", "").ToLowerInvariant()} \"github.com/microsoftgraph/{apiVersion}/{path.Replace(".", "/").ToLowerInvariant()}\"");
            }

            builder.AppendLine("\t  //other-imports"); // models version
            builder.AppendLine(")");
            builder.AppendLine("");
        }

        private static IEnumerable<String> getModelsPaths(SnippetCodeGraph codeGraph)
        {
            // check the body and its children recursively for the namespaces
            var nameSpaces = GetReferencedNamespaces(codeGraph);
            if (codeGraph.HasHeaders() || codeGraph.HasParameters() || codeGraph.HasOptions())
            {
                nameSpaces.Add(ProcessFinalNameSpaceName(codeGraph.Nodes.FirstOrDefault()?.Segment.ToLowerInvariant()));
            }
            return nameSpaces;
        }

        /// <summary>
        /// Returns a list of all the namespaces that are referenced in the body.
        /// </summary>
        public static HashSet<String> GetReferencedNamespaces(SnippetCodeGraph codeGraph)
        {

            HashSet<String> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (codeGraph.HasBody())
            {
                TraverseProperty(codeGraph.Body, x =>
                {
                    if (!string.IsNullOrWhiteSpace(x.NamespaceName))
                    {
                        var nameSpaceName = ProcessNameSpaceName(x.NamespaceName);
                        var pathSegments = nameSpaceName.Split(".");
                        var cleanNameSpace = pathSegments.FirstOrDefault()?.Equals("models") == true
                            ? nameSpaceName : pathSegments.FirstOrDefault();
                        result.Add(cleanNameSpace);
                    }
                });
            }
            return result;
        }

        private static void TraverseProperty(CodeProperty property, Action<CodeProperty> act)
        {
            act(property);
            if (property.Children != null)
            {
                foreach (var prop in property.Children)
                {
                    TraverseProperty(prop, act);
                }
            }
        }

        private static String ProcessNameSpaceName(String nameSpace)
        {
            if (String.IsNullOrEmpty(nameSpace))
                return "";

            // process function names and parameters
            var functionNameMatch = FunctionRegex.Match(nameSpace);
            if (functionNameMatch.Success)
            {
                var paramMatches = ParamRegex.Matches(functionNameMatch.Groups[2].Value);
                var paramNames = paramMatches.Cast<Match>().Select(static m => m.Groups[1].Value).ToList();

                return functionNameMatch.Groups[1].Value + "With" + string.Join("With", paramNames);
            }

            var processedName = (nameSpace.Split(".", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Equals("Me", StringComparison.OrdinalIgnoreCase) ? "Users" : x)
                .Aggregate(static (current, next) => current + "." + next)).Replace(".microsoft.graph", "");

            return processedName;
        }

        private static String ProcessFinalNameSpaceName(String nameSpace)
        {
            var nameSpaceName = ProcessNameSpaceName(nameSpace);
            var pathSegments = nameSpaceName.Split(".");
            return pathSegments.FirstOrDefault()?.Equals("models") == true
                ? nameSpaceName : pathSegments.FirstOrDefault();
        }

        private static Boolean hasPropertyOfType(SnippetCodeGraph codeGraph, PropertyType propertyType)
        {
            if (codeGraph.HasBody())
                return searchProperty(codeGraph.Body, propertyType);

            if (codeGraph.HasHeaders())
                return searchProperty(codeGraph.Headers, propertyType);

            if (codeGraph.HasParameters())
                return searchProperty(codeGraph.Parameters, propertyType);

            if (codeGraph.HasOptions())
                return searchProperty(codeGraph.Options, propertyType);

            return false;
        }

        private static Boolean searchProperty(IEnumerable<CodeProperty> properties, PropertyType propertyType)
        {
            return properties != null && propertyType == properties.FirstOrDefault(x => searchProperty(x, propertyType)).PropertyType;
        }


        private static Boolean searchProperty(CodeProperty property, PropertyType propertyType)
        {
            if (property.Children != null && property.Children.Count != 0)
            {
                var existingChild = property.Children.FirstOrDefault(x => x.PropertyType == propertyType);
                return propertyType == existingChild.PropertyType;
            }
            return property.PropertyType == propertyType;
        }

        private static void writeSnippet(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            writeHeadersAndOptions(codeGraph, builder);
            WriteBody(codeGraph, builder);
            builder.AppendLine("");

            builder.AppendLine("// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=go");
            WriteExecutionStatement(
                codeGraph,
                builder,
                codeGraph.HasBody() ? requestBodyVarName : default,
                codeGraph.HasHeaders() || codeGraph.HasOptions() || codeGraph.HasParameters() ? requestConfigurationVarName : "nil"
            );
        }

        private static void writeHeadersAndOptions(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (!codeGraph.HasHeaders() && !codeGraph.HasOptions() && !codeGraph.HasParameters()) return;

            var indentManager = new IndentManager();

            WriteHeader(codeGraph, builder, indentManager);
            WriteOptions(codeGraph, builder, indentManager);
            WriteParameters(codeGraph, builder, indentManager);

            var rootPath = ProcessFinalNameSpaceName(codeGraph.Nodes.FirstOrDefault()?.Segment).ToLowerInvariant();
            var className = $"graph{rootPath}.{GetNestedObjectName(codeGraph.Nodes)}RequestBuilder{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            builder.AppendLine($"{requestConfigurationVarName} := &{className}{{");
            indentManager.Indent();

            if (codeGraph.HasHeaders())
                builder.AppendLine($"{indentManager.GetIndent()}Headers: {requestHeadersVarName},");

            if (codeGraph.HasOptions())
                builder.AppendLine($"{indentManager.GetIndent()}Options: {optionsParameterVarName},");

            if (codeGraph.HasParameters())
                builder.AppendLine($"{indentManager.GetIndent()}QueryParameters: {requestParametersVarName},");

            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static void WriteHeader(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasHeaders()) return;

            builder.AppendLine($"{indentManager.GetIndent()}{requestHeadersVarName} := abstractions.NewRequestHeaders()");

            foreach (var param in codeGraph.Headers)
                builder.AppendLine($"{indentManager.GetIndent()}{requestHeadersVarName}.Add(\"{param.Name}\", \"{param.Value.EscapeQuotes()}\")");

            builder.AppendLine($"{indentManager.GetIndent()}");
        }

        private static void WriteOptions(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasOptions()) return;

            builder.AppendLine($"{indentManager.GetIndent()}{requestOptionsVarName} : {{");
            indentManager.Indent();
            foreach (var param in codeGraph.Options)
                builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.EscapeQuotes()}\",");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static void WriteParameters(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasParameters()) return;

            var nonArrayParams = codeGraph.Parameters.Where(static x => x.PropertyType != PropertyType.Array);

            if (nonArrayParams.Any())
                builder.AppendLine(string.Empty);

            foreach (var param in nonArrayParams)
            {
                builder.AppendLine($"request{indentManager.GetIndent()}{NormalizeJsonName(param.Name).ToFirstCharacterUpperCase()} := {evaluateParameter(param)}");
            }

            if (nonArrayParams.Any())
                builder.AppendLine(string.Empty);

            var rootPath = ProcessFinalNameSpaceName(codeGraph.Nodes.FirstOrDefault()?.Segment).ToLowerInvariant();
            var className = $"graph{rootPath}.{GetNestedObjectName(codeGraph.Nodes)}RequestBuilder{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            builder.AppendLine($"{indentManager.GetIndent()}{requestParametersVarName} := &{className}{{");
            indentManager.Indent();

            foreach (var param in codeGraph.Parameters)
            {
                if (param.PropertyType == PropertyType.Array)
                {
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(param.Name).ToFirstCharacterUpperCase()}: {evaluateParameter(param)},");
                }
                else
                {
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(param.Name).ToFirstCharacterUpperCase()}: &request{NormalizeJsonName(param.Name).ToFirstCharacterUpperCase()},");
                }
            }
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static string GetNestedObjectName(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            var enumeratedNodes = nodes?.ToList() ?? new List<OpenApiUrlTreeNode>();
            if (enumeratedNodes.Count == 0) return string.Empty;

            // if the first element is a collection index skip it
            var isCollection = enumeratedNodes[0].Segment.IsCollectionIndex();
            var isSingleElement = enumeratedNodes.Count == 1;
            var elementCount = enumeratedNodes.Count; // check if its a nested element

            var filteredNodes = enumeratedNodes;
            if (isCollection && !isSingleElement)
                filteredNodes = enumeratedNodes.Skip(2).ToList();
            else if (isCollection || elementCount > 2)
                filteredNodes = enumeratedNodes.Skip(1).ToList();

            if (filteredNodes.Count == 0) return string.Empty;
            return filteredNodes.Select(static x => x.Segment.IsCollectionIndex() ? "Item" : EscapeFunctionNames(x.Segment.ToFirstCharacterUpperCase()))
                        .Aggregate((x, y) =>
                        {
                            var w = elementCount < 3 && x.EndsWith('s') && y.Equals("Item") ? x.Remove(x.Length - 1, 1) : x;
                            w = "Me".Equals(w, StringComparison.Ordinal) ? "Item" : w;
                            return $"{w}{y}";
                        });
        }

        private static string EscapeFunctionNames(String objectName)
        {
            if (String.IsNullOrEmpty(objectName))
                return objectName;

            var match = FunctionRegex.Match(objectName);
            if (match.Success)
            {
                var paramMatches = ParamRegex.Matches(match.Groups[2].Value);
                var paramNames = paramMatches.Cast<Match>().Select(static m => m.Groups[1].Value.ToFirstCharacterUpperCase()).ToList();

                return paramNames.Count > 0 ? match.Groups[1].Value + "With" + string.Join("With", paramNames) : match.Groups[1].Value;
            }
            return objectName;
        }

        private static string evaluateParameter(CodeProperty param)
        {
            if (param.PropertyType == PropertyType.Array)
                return $"[] string {{{string.Join(",", param.Children.Select(static x => $"\"{x.Value}\"").ToList())}}}";
            else if (param.PropertyType == PropertyType.Boolean)
                return param.Value;
            else if (param.PropertyType == PropertyType.Int32)
                return $"int32({param.Value})";
            else
                return $"\"{param.Value.EscapeQuotes()}\"";
        }

        private static string NormalizeJsonName(string Name)
        {
            if ((!String.IsNullOrWhiteSpace(Name) && !Name.Substring(1).Equals("\"", StringComparison.OrdinalIgnoreCase)) && (Name.Contains('.') || Name.Contains('-')))
            {
                var propertyMatch = PropertyNameRegex.Match(Name);
                return propertyMatch.Success ? string.Join("", propertyMatch.Groups[1].Value.Split(".").Select(static x => x.ToFirstCharacterUpperCase())).ToFirstCharacterLowerCase() : $"\"{Name}\"";
            }

            return Name;
        }

        private static void WriteExecutionStatement(SnippetCodeGraph codeGraph, StringBuilder builder, params string[] parameters)
        {
            var methodName = codeGraph.GetSchemaFunctionCallPrefix();
            var parametersList = GetActionParametersList(parameters);
            var resultVarName = GetResultVarName(codeGraph);
            var returnStatement = codeGraph.HasReturnedBody() ? $"{resultVarName}, err := " : "";

            IndentManager indentManagerObjects = new IndentManager();
            CodeProperty defaultProerty = new CodeProperty();
            foreach (var param in codeGraph.PathParameters)
            {
                WriteCodeProperty("", builder, defaultProerty, param, indentManagerObjects, declarationOnly: true);
            }

            builder.AppendLine($"{returnStatement}{clientVarName}.{GetFluentApiPath(codeGraph.Nodes)}{methodName}(context.Background(), {parametersList})");
        }

        private static string GetResultVarName(SnippetCodeGraph codeGraph)
        {
            var path = codeGraph.Nodes.LastOrDefault(x => !x.IsParameter)?.Path?.Split("\\").Last(static x => !string.IsNullOrWhiteSpace(x)).Split("(").First()
                .Split(".")
                .Select(static s => s.ToFirstCharacterUpperCase())
                .Aggregate(static (a, b) => $"{a}{b}")
                .ToFirstCharacterLowerCase();
            return String.IsNullOrWhiteSpace(path) ? "result" : path;
        }

        private static void WriteBody(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (codeGraph.Body.PropertyType == PropertyType.Default) return;

            var indentManager = new IndentManager();

            if (codeGraph.Body.PropertyType == PropertyType.Binary)
            {
                builder.AppendLine($"{indentManager.GetIndent()}const {requestBodyVarName} := make([]byte, 0)");
            }
            else
            {
                // objects in namespace user have a prefix of item
                string bodyName = codeGraph.Body.NamespaceName.StartsWith("Me", StringComparison.OrdinalIgnoreCase) ? $"Item{codeGraph.Body.Name}" : codeGraph.Body.Name;
                builder.AppendLine($"{indentManager.GetIndent()}{requestBodyVarName} := graph{ProcessFinalNameSpaceName(codeGraph.Body.NamespaceName).Replace(".", "").ToLowerInvariant()}.New{bodyName.ToFirstCharacterUpperCase()}()");
                WriteCodePropertyObject(requestBodyVarName, builder, codeGraph.Body, indentManager);
            }
        }
        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(", ", nonEmptyParameters.Aggregate(static (a, b) => $"{a}, {b}"));
            else return string.Empty;
        }

        private static void WriteArrayProperty(string propertyAssignment, string objectName, StringBuilder builder, CodeProperty parentProperty, CodeProperty codeProperty, IndentManager indentManager)
        {
            var contentBuilder = new StringBuilder();
            var propertyName = NormalizeJsonName(codeProperty.Name.ToFirstCharacterLowerCase());

            var objectBuilder = new StringBuilder();
            IndentManager indentManagerObjects = new IndentManager();

            indentManager.Indent();
            var childPosition = 0;
            foreach (var child in codeProperty.Children)
            {
                if (child.PropertyType == PropertyType.Object)
                {
                    WriteCodeProperty(propertyAssignment, objectBuilder, codeProperty, child, indentManagerObjects, childPosition);
                    contentBuilder.AppendLine($"{indentManager.GetIndent()}{child.Name.ToFirstCharacterLowerCase()?.IndexSuffix(childPosition)},");
                }
                else
                {
                    WriteCodeProperty(propertyAssignment, contentBuilder, codeProperty, child, indentManager, childPosition);
                }
                childPosition++;
            }
            indentManager.Unindent();

            if (objectBuilder.Length > 0)
            {
                builder.AppendLine(Environment.NewLine);
                builder.AppendLine(objectBuilder.ToString());
            }

            var typeDefinition = codeProperty.TypeDefinition?.ToLowerInvariant()?.Trim();

            String typeName;
            if (NativeTypes.Contains(typeDefinition))
            {
                typeName = typeDefinition;
            }
            else if (formatPropertyName.TryGetValue(typeDefinition, out var type))
            {
                typeName = type;
            }
            else
            {
                typeName = $"graph{ProcessFinalNameSpaceName(codeProperty.NamespaceName).Replace(".", "").ToLowerInvariant()}.{codeProperty.TypeDefinition}able";
            }

            builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := []{typeName} {{");
            builder.AppendLine(contentBuilder.ToString().TrimEnd());
            builder.AppendLine($"{indentManager.GetIndent()}}}");
            if (parentProperty.PropertyType == PropertyType.Object)
                builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}({objectName})");

        }

        private static void WriteCodeProperty(string propertyAssignment, StringBuilder builder, CodeProperty codeProperty, CodeProperty child, IndentManager indentManager, int childPosition = 0, bool declarationOnly = false)
        {
            var isArray = codeProperty.PropertyType == PropertyType.Array;
            var isMap = codeProperty.PropertyType == PropertyType.Map;

            var propertyName = NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
            var objectName = isArray ? propertyName?.IndexSuffix(childPosition) : propertyName; // an indexed object name for  objects in an array
            switch (child.PropertyType)
            {
                case PropertyType.Object:
                    builder.AppendLine($"{objectName} := graph{ProcessFinalNameSpaceName(child.NamespaceName).Replace(".", "").ToLowerInvariant()}.New{child.TypeDefinition}()");
                    WriteCodePropertyObject(objectName, builder, child, indentManager);

                    if (!isArray)
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}({objectName})");

                    break;
                case PropertyType.Map:
                    builder.AppendLine($"{objectName} := map[string]interface{{}}{{");

                    indentManager.Indent();
                    WriteCodePropertyObject(propertyAssignment, builder, child, indentManager);
                    indentManager.Unindent();

                    builder.AppendLine("}");

                    if (isArray)
                        builder.AppendLine($"{indentManager.GetIndent()}{objectName}");
                    else
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}({objectName})");

                    break;
                case PropertyType.Array:
                    WriteArrayProperty(propertyAssignment, objectName, builder, codeProperty, child, indentManager);
                    break;
                case PropertyType.Guid:

                    if (!isArray)
                    {
                        builder.AppendLine($"{propertyName} := uuid.MustParse(\"{child.Value}\")");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    else
                    {
                        builder.AppendLine($"{indentManager.GetIndent()}uuid.MustParse(\"{child.Value}\"),");
                    }
                    break;
                case PropertyType.String:
                    WriteStringProperty(propertyAssignment, codeProperty, builder, indentManager, child, declarationOnly);
                    break;
                case PropertyType.Enum:
                    if (!String.IsNullOrWhiteSpace(child.Value))
                    {
                        var enumProperties = string.Join("_", child.Value.Split('.').Reverse().Select(static x => x.ToUpper()));
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := graphmodels.{enumProperties} ");
                        if (!declarationOnly) builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.DateTime:
                    builder.AppendLine($"{propertyName} , err := time.Parse(time.RFC3339, \"{child.Value}\")");
                    if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.Int32:
                case PropertyType.Int64:
                case PropertyType.Float32:
                case PropertyType.Float64:
                case PropertyType.Double:
                    WriteNumericProperty(propertyAssignment, isMap, builder, indentManager, child, declarationOnly);
                    break;
                case PropertyType.Base64Url:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := []byte(\"{child.Value.ToFirstCharacterLowerCase()}\")");
                    if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.Duration:
                    builder.AppendLine($"{propertyName} , err := abstractions.ParseISODuration(\"{child.Value}\")");
                    if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                default:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := {child.Value.ToFirstCharacterLowerCase()}");
                    if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
            }
        }
        private static void WriteStringProperty(string propertyAssignment, CodeProperty codeProperty, StringBuilder builder, IndentManager indentManager, CodeProperty child, bool declarationOnly)
        {

            var isArray = codeProperty.PropertyType == PropertyType.Array;
            var isMap = codeProperty.PropertyType == PropertyType.Map;

            var propertyName = NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());

            var propName = isMap ? $"\"{child.Name.ToFirstCharacterLowerCase()}\"" : NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
            if (isArray || String.IsNullOrWhiteSpace(propName))
                builder.AppendLine($"{indentManager.GetIndent()}\"{child.Value}\",");
            else if (isMap)
                builder.AppendLine($"{indentManager.GetIndent()}{child.Name.AddQuotes()} : \"{child.Value}\", ");
            else
            {
                builder.AppendLine($"{propertyName} := \"{child.Value}\"");
                if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
            }
        }

        private static void WriteNumericProperty(string propertyAssignment, bool isMap, StringBuilder builder, IndentManager indentManager, CodeProperty child, bool declarationOnly)
        {
            var propertyType = child.PropertyType switch
            {
                PropertyType.Int32 => "int32",
                PropertyType.Int64 => "int64",
                PropertyType.Float32 => "float32",
                PropertyType.Float64 => "float64",
                PropertyType.Double => "float64",
                _ => throw new ArgumentException("Unsupported child property")
            };

            var propertyName = NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
            if (isMap)
                builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : {propertyType}({child.Value}) , ");
            else
            {
                builder.AppendLine($"{propertyName} := {propertyType}({child.Value})");
                if (!declarationOnly) builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
            }
        }

        private static void WriteCodePropertyObject(string propertyAssignment, StringBuilder builder, CodeProperty codeProperty, IndentManager indentManager)
        {
            var childPosition = 0;
            foreach (var child in codeProperty.Children.Where(static x => !specialProperties.Contains(x.Name.Trim())))
                WriteCodeProperty(propertyAssignment, builder, codeProperty, child, indentManager, childPosition++);
        }

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            if (!(nodes?.Any() ?? false)) return string.Empty;
            var elements = nodes.Select(static (x, i) =>
            {
                if (x.Segment.IsCollectionIndex())
                {
                    var pathName = string.IsNullOrEmpty(x.Segment) ? x.Segment : x.Segment.ReplaceMultiple("", "{", "}").Split('-').Where(static s => !string.IsNullOrEmpty(s)).Select(static s => s.ToFirstCharacterUpperCase()).Aggregate(static (a, b) => $"By{a}{b}");
                    return $"{pathName ?? "ByTypeId"}{x.Segment.Replace("{", "(\"", StringComparison.OrdinalIgnoreCase).Replace("}", "\")", StringComparison.OrdinalIgnoreCase)}.";
                }

                var segmentValue = x.Segment.IsFunction() ? x.Segment.Split('.')
                                .Select(static s => s.ToFirstCharacterUpperCase())
                                .Aggregate(static (a, b) => $"{a}{b}") : x.Segment;
                var funcWithParams = segmentValue.GetFunctionWithParameters();
                if (!funcWithParams.Item1)
                    return segmentValue.ToFirstCharacterUpperCase() + "().";

                string withNames = string.Join("", funcWithParams.Item3.Keys.Select(static key => "With" + key.ToFirstCharacterUpperCase()));
                string varNames = String.Join(", ", funcWithParams.Item3.Keys.Select(item => "&" + item));
                return $"{funcWithParams.Item2.ToFirstCharacterUpperCase()}{withNames}({varNames}).";


            })
                        .Aggregate(new List<String>(), (current, next) =>
                        {
                            var element = next.Contains("ByTypeId", StringComparison.OrdinalIgnoreCase) ?
                            next.Replace("ByTypeId", $"By{current.Last().Replace("s().", string.Empty, StringComparison.OrdinalIgnoreCase)}Id") :
                            $"{next.Replace("$", string.Empty, StringComparison.OrdinalIgnoreCase).ToFirstCharacterUpperCase()}";

                            current.Add(element);
                            return current;
                        });

            return string.Join("", elements).Replace("()()", "()");
        }
    }
}
