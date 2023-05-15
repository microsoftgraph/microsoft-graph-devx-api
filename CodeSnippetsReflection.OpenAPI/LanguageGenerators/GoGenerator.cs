using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Expressions;
using System.Diagnostics.Metrics;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class GoGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string clientVarName = "graphClient";
        private const string clientVarType = "GraphServiceClientWithCredentials";
        private const string clientFactoryVariables = "cred, scopes";
        private const string requestBodyVarName = "requestBody";
        private const string requestHeadersVarName = "headers";
        private const string optionsParameterVarName = "options";
        private const string requestOptionsVarName = "options";
        private const string requestParametersVarName = "requestParameters";
        private const string requestConfigurationVarName = "configuration";

        private static IImmutableSet<string> specialProperties = ImmutableHashSet.Create("@odata.type");

        private static IImmutableSet<string> NativeTypes = GetNativeTypes();

        private static readonly Regex PropertyNameRegex = new Regex(@"@(.*)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        static IImmutableSet<string> GetNativeTypes()
        {
            return ImmutableHashSet.Create("string", "int", "float");
        }

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            if (snippetModel == null) throw new ArgumentNullException("Argument snippetModel cannot be null");

            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder();
            snippetBuilder.AppendLine("");

            writeImportStatements(codeGraph, snippetBuilder);
            writeSnippet(codeGraph, snippetBuilder);

            return snippetBuilder.ToString();
        }

        private static void writeImportStatements(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
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
                builder.AppendLine($"\t  graph{path} \"github.com/microsoftgraph/{apiVersion}/{path}\"");
            }

            var configPath = evaluateConfigPath(codeGraph);
            if (!String.IsNullOrWhiteSpace(configPath) && !models.Contains(configPath))
                builder.AppendLine($"\t  graph{configPath} \"github.com/microsoftgraph/{apiVersion}/{configPath}\"");

            builder.AppendLine("\t  //other-imports"); // models version
            builder.AppendLine(")");
            builder.AppendLine("");
        }

        private static string evaluateConfigPath(SnippetCodeGraph codeGraph)
        {
            if (codeGraph.RequiresRequestConfig())
            {
                var path = codeGraph.Nodes.First().Segment.ToLowerInvariant();
                return path.Equals("me", StringComparison.OrdinalIgnoreCase) ? "users" : path;
            }

            return string.Empty;
        }


        private static IEnumerable<String> getModelsPaths(SnippetCodeGraph codeGraph)
        {
            // check the body and its children recursively for the namespaces
            return codeGraph.GetReferencedNamespaces(true).Select(evaluateNameSpaceName);
        }


        private static string evaluateModelPath(SnippetCodeGraph codeGraph)
        {
            if (codeGraph.HasJsonBody())
            {
                var path = codeGraph.Body.NamespaceName.Replace("microsoft.graph", "").Replace(".", "/").Replace("//","/").ToLowerInvariant().Split("/").FirstOrDefault();
                if(path == null) return path;
                return path.EndsWith("/") ? path.Remove(path.Length - 1, 1) : path;
            }

            return string.Empty;
        }

        private static string evaluateNameSpaceName(string nameSpace)
        {
            return nameSpace.Split(".").FirstOrDefault()?.ToLowerInvariant()?.Replace(".microsoft.graph", "");
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
            if (property.Children != null && property.Children.Any())
            {
                var existingChild = property.Children.FirstOrDefault(x => x.PropertyType == propertyType);
                return propertyType == existingChild.PropertyType;
            }
            return property.PropertyType == propertyType;
        }

        private static void writeSnippet(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            builder.AppendLine($"{clientVarName}, err := msgraphsdk.New{clientVarType}({clientFactoryVariables}){Environment.NewLine}{Environment.NewLine}");
            writeHeadersAndOptions(codeGraph, builder);
            WriteBody(codeGraph, builder);
            builder.AppendLine("");

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

            var className = $"graphconfig.{GetNestedObjectName(codeGraph.Nodes)}RequestBuilder{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
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

            var className = $"graphconfig.{GetNestedObjectName(codeGraph.Nodes)}RequestBuilder{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
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
            if (!(nodes?.Any() ?? false)) return string.Empty;
            // if the first element is a collection index skip it
            var fileterdNodes = (nodes.First().Segment.IsCollectionIndex()) ? nodes.Skip(1) : nodes;
            return fileterdNodes.Select(static x =>
            {
                if (x.Segment.IsCollectionIndex())
                    return "Item";
                else
                    return x.Segment.ToFirstCharacterUpperCase();
            })
                        .Aggregate(static (x, y) =>
                        {
                            var w = x.EndsWith("s") && y.Equals("Item") ? x.Remove(x.Length - 1, 1) : x;
                            w = "Me".Equals(w, StringComparison.Ordinal) ? "Item" : w;
                            return $"{w}{y}";
                        });
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
                return propertyMatch.Success ? string.Join("",propertyMatch.Groups[1].Value.Split(".").Select(static x => x.ToFirstCharacterUpperCase())).ToFirstCharacterLowerCase() : $"\"{Name}\"";
            }

            return Name;
        }

        private static void WriteExecutionStatement(SnippetCodeGraph codeGraph, StringBuilder builder, params string[] parameters)
        {
            var methodName = $"{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}";

            var parametersList = GetActionParametersList(parameters);
            var returnStatement = codeGraph.HasReturnedBody() ? "result, err := " : "";
            builder.AppendLine($"{returnStatement}{clientVarName}.{GetFluentApiPath(codeGraph.Nodes)}{methodName}(context.Background(), {parametersList})");
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
                builder.AppendLine($"{indentManager.GetIndent()}{requestBodyVarName} := graphmodels.New{codeGraph.Body.Name.ToFirstCharacterUpperCase()}()");
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

            var typeName = NativeTypes.Contains(codeProperty.TypeDefinition?.ToLowerInvariant()?.Trim()) ? codeProperty.TypeDefinition?.ToLowerInvariant() : $"graphmodels.{codeProperty.TypeDefinition}able";
            builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := []{typeName} {{");
            builder.AppendLine(contentBuilder.ToString());
            builder.AppendLine($"{indentManager.GetIndent()}}}");
            if (parentProperty.PropertyType == PropertyType.Object)
                builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}({objectName})");

        }

        private static void WriteCodeProperty(string propertyAssignment, StringBuilder builder, CodeProperty codeProperty, CodeProperty child, IndentManager indentManager, int childPosition = 0)
        {
            var isArray = codeProperty.PropertyType == PropertyType.Array;
            var isMap = codeProperty.PropertyType == PropertyType.Map;

            var propertyName = NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
            var objectName = isArray ? propertyName?.IndexSuffix(childPosition) : propertyName; // an indexed object name for  objects in an array
            switch (child.PropertyType)
            {
                case PropertyType.Object:
                    builder.AppendLine($"{objectName} := graphmodels.New{child.TypeDefinition}()");
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
                    builder.AppendLine($"{propertyName} := uuid.MustParse(\"{child.Value}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.String:
                    WriteStringProperty(propertyAssignment, codeProperty, builder, indentManager, child);
                    break;
                case PropertyType.Enum:
                    if (!String.IsNullOrWhiteSpace(child.Value))
                    {
                        var enumProperties = string.Join("_", child.Value.Split('.').Reverse().Select(static x => x.ToUpper()));
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := graphmodels.{enumProperties} ");
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.DateTime:
                    builder.AppendLine($"{propertyName} , err := time.Parse(time.RFC3339, \"{child.Value}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.Int32:
                case PropertyType.Int64:
                case PropertyType.Float32:
                case PropertyType.Float64:
                case PropertyType.Double:
                    WriteNumericProperty(propertyAssignment, isMap, builder, indentManager, child);
                    break;
                case PropertyType.Base64Url:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := []byte(\"{child.Value.ToFirstCharacterLowerCase()}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.Duration:
                    builder.AppendLine($"{propertyName} , err := abstractions.ParseISODuration(\"{child.Value}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                default:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := {child.Value.ToFirstCharacterLowerCase()}");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
            }
        }
        private static void WriteStringProperty(string propertyAssignment, CodeProperty codeProperty, StringBuilder builder, IndentManager indentManager, CodeProperty child)
        {

            var isArray = codeProperty.PropertyType == PropertyType.Array;
            var isMap = codeProperty.PropertyType == PropertyType.Map;

            var propertyName = NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());

            var propName = isMap ? $"\"{child.Name.ToFirstCharacterLowerCase()}\"" : NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
            if (isArray || String.IsNullOrWhiteSpace(propName))
                builder.AppendLine($"{indentManager.GetIndent()}\"{child.Value}\",");
            else if (isMap)
                builder.AppendLine($"{indentManager.GetIndent()}{propertyName.AddQuotes()} : \"{child.Value}\", ");
            else
            {
                builder.AppendLine($"{propertyName} := \"{child.Value}\"");
                builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
            }
        }

        private static void WriteNumericProperty(string propertyAssignment, bool isMap, StringBuilder builder, IndentManager indentManager, CodeProperty child)
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
                builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
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
                    return $"ByTypeId{x.Segment.Replace("{", "(\"").Replace("}", "\")")}.";
                else if (x.Segment.IsFunction())
                    return x.Segment.Split('.')
                                    .Select(static s => s.ToFirstCharacterUpperCase())
                                    .Aggregate(static (a, b) => $"{a}{b}") + "().";
                return x.Segment.ToFirstCharacterUpperCase() + "().";
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
