using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators
{
    public class TypeScriptGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for TypeScript
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <returns>String of the snippet in Javascript code</returns>
        ///

        private const string ClientVarName = "graphServiceClient";
        private const string ClientVarType = "GraphServiceClient";
        private const string RequestHeadersVarName = "headers";
        private const string RequestOptionsVarName = "options";
        private const string RequestConfigurationVarName = "configuration";
        private const string RequestParametersVarName = "requestParameters";
        private const string RequestBodyVarName = "requestBody";

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            if (snippetModel == null) throw new ArgumentNullException("Argument snippetModel cannot be null");

            var codeGraph = ModelGraphBuilder.BuildCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder(
                                    "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                                    $"const {ClientVarName} = {ClientVarType}.init({{authProvider}});{Environment.NewLine}{Environment.NewLine}");

            writeSnippet(codeGraph, snippetBuilder);

            return snippetBuilder.ToString();
        }

        private static void writeSnippet(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            writeHeadersAndOptions(codeGraph, builder);
            WriteParameters(codeGraph, builder);
            WriteBody(codeGraph, builder);
            builder.AppendLine("");

            WriteExecutionStatement(
                codeGraph,
                builder,
                codeGraph.HasBody() ? RequestBodyVarName : default,
                codeGraph.HasParameters() ? RequestParametersVarName : default,
                codeGraph.HasHeaders() || codeGraph.HasOptions() ? RequestConfigurationVarName : default
            );
        }

        private static void writeHeadersAndOptions(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (!codeGraph.HasHeaders() && !codeGraph.HasOptions()) return;

            var indentManager = new IndentManager();
            builder.AppendLine($"const {RequestConfigurationVarName} = {{");
            indentManager.Indent();
            WriteHeader(codeGraph, builder, indentManager);
            WriteOptions(codeGraph, builder, indentManager);
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}};");
        }

        private static void WriteHeader(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (codeGraph.HasHeaders())
            {
                builder.AppendLine($"{indentManager.GetIndent()}{RequestHeadersVarName} : {{");
                indentManager.Indent();
                foreach (var param in codeGraph.Headers)
                    builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.Replace("\"", "\\\"")}\",");
                indentManager.Unindent();
                builder.AppendLine($"{indentManager.GetIndent()}}}");
            }
        }
        private static void WriteOptions(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (codeGraph.HasOptions())
            {
                if (codeGraph.HasHeaders())
                    builder.Append(",");

                builder.AppendLine($"{indentManager.GetIndent()}{RequestOptionsVarName} : {{");
                indentManager.Indent();
                foreach (var param in codeGraph.Options)
                    builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.Replace("\"", "\\\"")}\",");
                indentManager.Unindent();
                builder.AppendLine($"{indentManager.GetIndent()}}}");
            }
        }

        private static void WriteParameters(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (!codeGraph.HasParameters()) return;

            var indentManager = new IndentManager();
            builder.AppendLine($"const {RequestParametersVarName} = {{");
            indentManager.Indent();
            foreach (var param in codeGraph.Parameters)
                builder.AppendLine($"{indentManager.GetIndent()}{param.Name} : {param.Value},");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}};");
        }

        private static void WriteExecutionStatement(SnippetCodeGraph codeGraph, StringBuilder builder, params string[] parameters)
        {
            var methodName = codeGraph.HttpMethod.ToString().ToLower();
            var responseAssignment = codeGraph.ResponseSchema == null ? string.Empty : "const result = ";

            var parametersList = GetActionParametersList(parameters);

            var indentManager = new IndentManager();
            builder.AppendLine($"{responseAssignment}async () => {{");
            indentManager.Indent();
            builder.AppendLine($"{indentManager.GetIndent()}await {ClientVarName}.{GetFluentApiPath(codeGraph.Nodes)}.{methodName}({parametersList});");
            indentManager.Unindent();
            builder.AppendLine($"}}");
        }

        private static void WriteBody(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (codeGraph.Body.PropertyType == PropertyType.Default) return;

            var indentManager = new IndentManager();

            if (codeGraph.Body.PropertyType == PropertyType.Binary)
            {
                builder.AppendLine($"{indentManager.GetIndent()}const {RequestBodyVarName} = new WebStream();");
            }
            else
            {
                builder.AppendLine($"{indentManager.GetIndent()}const {RequestBodyVarName} : {codeGraph.Body.Name} = {{");
                indentManager.Indent();
                WriteCodePropertyObject(builder, codeGraph.Body, indentManager);
                indentManager.Unindent();
                builder.AppendLine($"}};");
            }
        }

        private static string NormalizeJsonName(string Name)
        {
            if (Name.Contains(".")) return $"\"{Name}\"";

            return Name;
        }
        
        private static void WriteCodePropertyObject(StringBuilder builder, CodeProperty codeProperty, IndentManager indentManager)
        {
            foreach (var child in codeProperty.Children)
            {
                switch (child.PropertyType)
                {
                    case PropertyType.Object:
                    case PropertyType.Map:
                        if (codeProperty.PropertyType == PropertyType.Array)
                        {
                            builder.AppendLine($"{indentManager.GetIndent()}{{");
                        }
                        else
                        {
                            builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} : {{");
                        }

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
                    case PropertyType.String:
                        var propName = codeProperty.PropertyType == PropertyType.Map ? $"\"{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())}\"" : child.Name.ToFirstCharacterLowerCase();
                        if (String.IsNullOrWhiteSpace(propName))
                        {
                            builder.AppendLine($"{indentManager.GetIndent()}\"{child.Value}\",");
                        }
                        else
                        {
                            builder.AppendLine($"{indentManager.GetIndent()}{propName} : \"{child.Value}\",");
                        }
                        break;
                    case PropertyType.Enum:
                        builder.AppendLine($"{indentManager.GetIndent()}{child.Name.ToFirstCharacterLowerCase()} : {child.Value},");
                        break;
                    case PropertyType.Date:
                        builder.AppendLine($"{indentManager.GetIndent()}{child.Name} : new Date(\"{child.Value}\"),");
                        break;
                    default:
                        builder.AppendLine($"{indentManager.GetIndent()}{child.Name.ToFirstCharacterLowerCase()} : {child.Value.ToFirstCharacterLowerCase()},");
                        break;
                }
            }
        }

        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
            else return string.Empty;
        }

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes)
        {
            if (!(nodes?.Any() ?? false)) return string.Empty;
            return nodes.Select(x => {
                if (x.Segment.IsCollectionIndex())
                    return $"ById{x.Segment.Replace("{", "(\"").Replace("}", "\")")}";
                else if (x.Segment.IsFunction())
                    return x.Segment.Split('.').Last().ToFirstCharacterLowerCase();
                return x.Segment.ToFirstCharacterLowerCase();
            })
                        .Aggregate((x, y) => {
                            var dot = y.StartsWith("ById") ?
                                            string.Empty :
                                            ".";
                            return $"{x}{dot}{y}";
                        });
        }
    }
}
