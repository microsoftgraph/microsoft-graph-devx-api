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
    public class TypeScriptGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for TypeScript
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <returns>String of the snippet in TypeScript code</returns>
        ///
        private const string ClientVarName = "graphServiceClient";

        private const string RequestHeadersVarName = "headers";
        private const string RequestOptionsVarName = "options";
        private const string RequestConfigurationVarName = "configuration";
        private const string RequestParametersVarName = "queryParameters";
        private const string RequestBodyVarName = "requestBody";

        private static readonly Regex FunctionRegex =
            new Regex(@"(\w+)\(([^)]*)\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        private static readonly Regex ParamRegex =
            new Regex(@"(\w+)\s*=\s*'[^']*'", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            if (snippetModel == null) throw new ArgumentNullException("Argument snippetModel cannot be null");

            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder(
                "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine);

            WriteImportStatements(codeGraph, snippetBuilder);
            WriteSnippet(codeGraph, snippetBuilder);

            return snippetBuilder.ToString();
        }

        private static void WriteImportStatements(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            builder.AppendLine("// Dependencies");
            var apiVersion = "v1.0".Equals(codeGraph.ApiVersion, StringComparison.Ordinal) ? "msgraph-sdk" : "msgraph-beta-sdk";

            var initialPackage = codeGraph.Nodes.First().Segment.ToLowerInvariant();
            if (initialPackage.Equals("me", StringComparison.OrdinalIgnoreCase))
            {
                initialPackage = "users";
            }

            builder.AppendLine($"import \"@microsoft/{apiVersion}-{initialPackage}\";"); // api version

            // add models
            var models = GetImportNamespaces(apiVersion, codeGraph);
            foreach (var path in models)
            {
                var pathModels = path.Value.Select(static x => x.ToFirstCharacterUpperCase())
                    .Where(static s => !string.IsNullOrEmpty(s)).Aggregate(static (a, b) => $"{a}, {b}");
                builder.AppendLine($"import {{ {pathModels} }} from \"{path.Key}\";");
            }

            builder.AppendLine("//other-imports"); // models version
            builder.AppendLine("");
        }

        private static string GetCleanNamespaceName(string namespaceName)
        {
            var nameSpaceName = ProcessNameSpaceName(namespaceName);
            var pathSegments = nameSpaceName.Split(".");
            return pathSegments.FirstOrDefault()?.Equals("models") == true
                ? nameSpaceName
                : pathSegments.FirstOrDefault();
        }

        private static void AddNamespace(Dictionary<string, HashSet<string>> result, string nameSpace,
            string valueToAdd)
        {
            if (result.TryGetValue(nameSpace, out var value))
            {
                value.Add(valueToAdd);
            }
            else
            {
                result.Add(nameSpace, [valueToAdd]);
            }
        }

        private static Dictionary<String, HashSet<String>> GetImportNamespaces(String apiVersion,
            SnippetCodeGraph codeGraph)
        {
            Dictionary<String, HashSet<String>> result =
                new Dictionary<string, HashSet<String>>(StringComparer.OrdinalIgnoreCase);
            if (codeGraph.HasBody())
            {
                var modelsNameSpace = $"@microsoft/{apiVersion}/models";
                TraverseProperty(codeGraph.Body, x =>
                {
                    switch (x.PropertyType)
                    {
                        case PropertyType.DateOnly when !string.IsNullOrEmpty(x.Value):
                            AddNamespace(result, "@microsoft/kiota-abstractions", "DateOnly");
                            break;
                        case PropertyType.TimeOnly when !string.IsNullOrEmpty(x.Value):
                            AddNamespace(result, "@microsoft/kiota-abstractions", "TimeOnly");
                            break;
                        case PropertyType.Enum when !string.IsNullOrEmpty(x.Value) &&
                                                    !string.IsNullOrWhiteSpace(x.NamespaceName):
                            var enumNameSpace = GetCleanNamespaceName(x.NamespaceName);
                            var enumType = x.Value.Split(".").First().ToFirstCharacterUpperCase() + "Object";
                            AddNamespace(result, $"@microsoft/{apiVersion}/{enumNameSpace.ToLowerInvariant()}",
                                enumType);
                            break;
                        case PropertyType.Object when !string.IsNullOrWhiteSpace(x.NamespaceName) &&
                                                      !result.ContainsKey(modelsNameSpace):
                            var formatNameSpace = x.NamespaceName.Split(".").Select(static x =>
                            {
                                var result = x.ToFirstCharacterLowerCase();
                                return result.Equals("me", StringComparison.OrdinalIgnoreCase) ? "users/item" : result;
                            }).Aggregate(static (x, y) => $"{x}/{y}");
                            var objectCleanNameSpace = GetCleanNamespaceName(x.NamespaceName);

                            var objectNameSpace = objectCleanNameSpace.ToFirstCharacterLowerCase().Equals("models", StringComparison.OrdinalIgnoreCase)
                                ? $"@microsoft/{apiVersion}/{objectCleanNameSpace.ToLowerInvariant()}"
                                : $"@microsoft/{apiVersion}-{objectCleanNameSpace.ToLowerInvariant()}";

                            var lastNameSpace = objectCleanNameSpace.ToFirstCharacterLowerCase().Equals("models", StringComparison.OrdinalIgnoreCase)
                                ? objectNameSpace
                                : $"{objectNameSpace}/{formatNameSpace.ToFirstCharacterLowerCase()}";
                            AddNamespace(result, lastNameSpace, x.Name);
                            break;
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

        private static void WriteSnippet(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            writeHeadersAndOptions(codeGraph, builder);
            WriteBody(codeGraph, builder);
            builder.AppendLine("");

            builder.AppendLine(
                "// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript");
            builder.AppendLine("");

            WriteExecutionStatement(
                codeGraph,
                builder,
                codeGraph.HasBody() ? RequestBodyVarName : default,
                codeGraph.HasHeaders() || codeGraph.HasOptions() || codeGraph.HasParameters()
                    ? RequestConfigurationVarName
                    : default
            );
        }

        private static void writeHeadersAndOptions(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (!codeGraph.HasHeaders() && !codeGraph.HasOptions() && !codeGraph.HasParameters()) return;

            var indentManager = new IndentManager();
            builder.AppendLine($"const {RequestConfigurationVarName} = {{");
            indentManager.Indent();
            WriteHeader(codeGraph, builder, indentManager);
            WriteOptions(codeGraph, builder, indentManager);
            WriteParameters(codeGraph, builder, indentManager);
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}};");
        }

        private static void WriteHeader(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasHeaders()) return;

            builder.AppendLine($"{indentManager.GetIndent()}{RequestHeadersVarName} : {{");
            indentManager.Indent();
            foreach (var param in codeGraph.Headers)
                builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.EscapeQuotes()}\",");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static void WriteOptions(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasOptions()) return;

            if (codeGraph.HasHeaders())
                builder.Append(',');

            builder.AppendLine($"{indentManager.GetIndent()}{RequestOptionsVarName} : {{");
            indentManager.Indent();
            foreach (var param in codeGraph.Options)
                builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.EscapeQuotes()}\",");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static void WriteParameters(SnippetCodeGraph codeGraph, StringBuilder builder,
            IndentManager indentManager)
        {
            if (!codeGraph.HasParameters()) return;

            if (codeGraph.HasHeaders() || codeGraph.HasOptions())
                builder.Append(',');

            builder.AppendLine($"{indentManager.GetIndent()}{RequestParametersVarName} : {{");
            indentManager.Indent();
            foreach (var param in codeGraph.Parameters)
                builder.AppendLine(
                    $"{indentManager.GetIndent()}{NormalizeJsonName(param.Name)}: {evaluateParameter(param)},");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static string evaluateParameter(CodeProperty param)
        {
            if (param.PropertyType == PropertyType.Array)
                return $"[{string.Join(",", param.Children.Select(static x => $"\"{x.Value}\"").ToList())}]";
            else if (param.PropertyType == PropertyType.Boolean || param.PropertyType == PropertyType.Int32)
                return param.Value;
            else
                return $"\"{param.Value.EscapeQuotes()}\"";
        }

        private static void WriteExecutionStatement(SnippetCodeGraph codeGraph, StringBuilder builder,
            params string[] parameters)
        {
            var methodName = codeGraph.HttpMethod.ToString().ToLower();
            var responseAssignment = codeGraph.ResponseSchema == null ? string.Empty : "const result = ";

            var parametersList = GetActionParametersList(parameters);

            var indentManager = new IndentManager();
            builder.AppendLine($"{responseAssignment}async () => {{");
            indentManager.Indent();
            builder.AppendLine(
                $"{indentManager.GetIndent()}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{methodName}({parametersList});");
            indentManager.Unindent();
            builder.AppendLine($"}}");
        }

        private static void WriteBody(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (codeGraph.Body.PropertyType == PropertyType.Default) return;

            var indentManager = new IndentManager();

            if (codeGraph.Body.PropertyType == PropertyType.Binary)
            {
                builder.AppendLine(
                    $"{indentManager.GetIndent()}const {RequestBodyVarName} = new ArrayBuffer({codeGraph.Body.Value.Length});");
            }
            else
            {
                builder.AppendLine(
                    $"{indentManager.GetIndent()}const {RequestBodyVarName} : {codeGraph.Body.Name} = {{");
                indentManager.Indent();
                WriteCodePropertyObject(builder, codeGraph.Body, indentManager);
                indentManager.Unindent();
                builder.AppendLine($"}};");
            }
        }

        private static string NormalizeJsonName(string Name)
        {
            return (!String.IsNullOrWhiteSpace(Name) && Name.Substring(1) != "\"") &&
                   (Name.Contains('.') || Name.Contains('-'))
                ? $"\"{Name}\""
                : Name;
        }

        private static void WriteCodePropertyObject(StringBuilder builder, CodeProperty codeProperty,
            IndentManager indentManager)
        {
            var isArray = codeProperty.PropertyType == PropertyType.Array;
            foreach (var child in codeProperty.Children)
            {
                switch (child.PropertyType)
                {
                    case PropertyType.Object:
                    case PropertyType.Map:
                        if (isArray)
                            builder.AppendLine($"{indentManager.GetIndent()}{{");
                        else
                            builder.AppendLine(
                                $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} : {{");

                        indentManager.Indent();
                        WriteCodePropertyObject(builder, child, indentManager);
                        indentManager.Unindent();
                        builder.AppendLine($"{indentManager.GetIndent()}}},");

                        break;
                    case PropertyType.Array:

                        builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name)} : [");
                        indentManager.Indent();
                        WriteCodePropertyObject(builder, child, indentManager);
                        indentManager.Unindent();
                        builder.AppendLine($"{indentManager.GetIndent()}],");

                        break;
                    case PropertyType.Guid:
                    case PropertyType.String:
                        var propName = codeProperty.PropertyType == PropertyType.Map
                            ? $"\"{child.Name.ToFirstCharacterLowerCase()}\""
                            : NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
                        if (isArray || String.IsNullOrWhiteSpace(propName))
                            builder.AppendLine($"{indentManager.GetIndent()}\"{child.Value}\",");
                        else
                            builder.AppendLine($"{indentManager.GetIndent()}{propName} : \"{child.Value}\",");
                        break;
                    case PropertyType.Enum:
                        if (!String.IsNullOrWhiteSpace(child.Value))
                        {
                            var enumParts = child.Value.Split('.');
                            var enumValue = $"{enumParts[0].ToFirstCharacterUpperCase()}Object.{enumParts.Last()}";
                            builder.AppendLine(
                                $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} : {enumValue},");
                        }

                        break;
                    case PropertyType.DateTime:
                        builder.AppendLine(
                            $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name)} : new Date(\"{child.Value}\"),");
                        break;
                    case PropertyType.DateOnly:
                        builder.AppendLine(
                            $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name)} : DateOnly.parse(\"{child.Value}\"),");
                        break;
                    case PropertyType.Base64Url:
                        builder.AppendLine(
                            $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} : \"{child.Value.ToFirstCharacterLowerCase()}\",");
                        break;
                    default:
                        builder.AppendLine(
                            $"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} : {child.Value.ToFirstCharacterLowerCase()},");
                        break;
                }
            }
        }

        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(static p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(", ", nonEmptyParameters.Aggregate(static (a, b) => $"{a}, {b}"));
            return string.Empty;
        }

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            List<OpenApiUrlTreeNode> enumeratedNodes = nodes.ToList();
            if (!(enumeratedNodes?.Any() ?? false)) return string.Empty;

            return enumeratedNodes.Select((part, index) =>
                {
                    if (part.Segment.IsCollectionIndex())
                    {
                        var node = part.Segment.Replace("{", "").Replace("}", "");
                        var nodeName =
                            $"by{node.Split("-").Select(static  x => x.ToFirstCharacterUpperCase()).Aggregate(static  (x, y) => $"{x}{y}")}";
                        return $"{nodeName}(\"{node}\")";
                    }

                    if (part.Segment.IsFunction())
                        return part.Segment.Split('.')
                            .Select(static s => s.ToFirstCharacterUpperCase())
                            .Aggregate(static (a, b) => $"{a}{b}").ToFirstCharacterLowerCase();
                    return part.Segment.ToFirstCharacterLowerCase();
                })
                .Where(static s => !string.IsNullOrEmpty(s)) // Remove any empty entries
                .Aggregate(static (x, y) => $"{x}.{y}");
        }
    }
}
