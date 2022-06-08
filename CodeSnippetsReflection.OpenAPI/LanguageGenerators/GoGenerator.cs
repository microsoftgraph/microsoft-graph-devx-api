using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
    public class GoGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string clientVarName = "graphClient";
        private const string clientVarType = "GraphServiceClient";
        private const string httpCoreVarName = "requestAdapter";
        private const string requestBodyVarName = "requestBody";
        private const string requestHeadersVarName = "headers";
        private const string optionsParameterVarName = "options";
        private const string requestOptionsVarName = "options";
        private const string requestParametersVarName = "requestParameters";
        private const string requestConfigurationVarName = "configuration";
        private static IImmutableSet<string> NativeTypes;

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            if (snippetModel == null) throw new ArgumentNullException("Argument snippetModel cannot be null");

            if (NativeTypes == null)
                NativeTypes = ImmutableHashSet.Create("string", "int", "float");

            var codeGraph = new SnippetCodeGraph(snippetModel);
            var snippetBuilder = new StringBuilder(
                                    "//THE GO SDK IS IN PREVIEW. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                                    $"{clientVarName} := msgraphsdk.New{clientVarType}({httpCoreVarName}){Environment.NewLine}{Environment.NewLine}");

            writeSnippet(codeGraph, snippetBuilder);

            return snippetBuilder.ToString();
        }
        
        private static void writeSnippet(SnippetCodeGraph codeGraph, StringBuilder builder){
            writeHeadersAndOptions(codeGraph, builder);
            WriteBody(codeGraph, builder);
            builder.AppendLine("");

            WriteExecutionStatement(
                codeGraph,
                builder,
                codeGraph.HasHeaders() || codeGraph.HasOptions() || codeGraph.HasParameters() ,
                codeGraph.HasBody() ? requestBodyVarName : default,
                codeGraph.HasHeaders() || codeGraph.HasOptions() || codeGraph.HasParameters() ? requestConfigurationVarName : default
            );
        }

        private static void writeHeadersAndOptions(SnippetCodeGraph codeGraph, StringBuilder builder)
        {
            if (!codeGraph.HasHeaders() && !codeGraph.HasOptions() && !codeGraph.HasParameters()) return;

            var indentManager = new IndentManager();
            
            WriteHeader(codeGraph, builder, indentManager);
            WriteOptions(codeGraph, builder, indentManager);
            WriteParameters(codeGraph, builder, indentManager);

            var className = $"graphconfig.{codeGraph.Nodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
            builder.AppendLine($"{requestConfigurationVarName} := &{className}{{");
            indentManager.Indent();

            if(codeGraph.HasHeaders())
                builder.AppendLine($"{indentManager.GetIndent()}Headers: {requestHeadersVarName},");

            if(codeGraph.HasOptions())
                builder.AppendLine($"{indentManager.GetIndent()}Options: {optionsParameterVarName},");

            if(codeGraph.HasParameters())
                builder.AppendLine($"{indentManager.GetIndent()}QueryParameters: {requestParametersVarName},");

            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static void WriteHeader(SnippetCodeGraph codeGraph, StringBuilder builder, IndentManager indentManager)
        {
            if (!codeGraph.HasHeaders()) return;

            builder.AppendLine($"{indentManager.GetIndent()}{requestHeadersVarName} := map[string]string{{");
            indentManager.Indent();
            foreach (var param in codeGraph.Headers)
                builder.AppendLine($"{indentManager.GetIndent()}\"{param.Name}\": \"{param.Value.EscapeQuotes()}\",");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
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

            var className = $"graphconfig.{codeGraph.Nodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{codeGraph.HttpMethod.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
            builder.AppendLine($"{indentManager.GetIndent()}{requestParametersVarName} := &{className}{{");
            indentManager.Indent();
            foreach (var param in codeGraph.Parameters)
                builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(param.Name).ToFirstCharacterUpperCase()}: {evaluateParameter(param)},");
            indentManager.Unindent();
            builder.AppendLine($"{indentManager.GetIndent()}}}");
        }

        private static string evaluateParameter(CodeProperty param){
            if(param.PropertyType == PropertyType.Array)
                return $"[] string {{{string.Join(",", param.Children.Select(x =>  $"\"{x.Value}\"" ).ToList())}}}";
            else if (param.PropertyType == PropertyType.Boolean || param.PropertyType == PropertyType.Int32)
                return param.Value;
            else
                return $"\"{param.Value.EscapeQuotes()}\"";
        }

        private static string NormalizeJsonName(string Name)
        {
            return (!String.IsNullOrWhiteSpace(Name) && Name.Substring(1) != "\"") && (Name.Contains('.') || Name.Contains('-')) ? $"\"{Name}\"" : Name;
        }

        private static void WriteExecutionStatement(SnippetCodeGraph codeGraph, StringBuilder builder, Boolean hasOptions, params string[] parameters)
        {
            var methodName = $"{codeGraph.HttpMethod.ToString().ToLower().ToFirstCharacterUpperCase()}{(hasOptions ? "WithRequestConfigurationAndResponseHandler" : "")}";

            var parametersList = GetActionParametersList(parameters);
            if(hasOptions)
                parametersList += ", nil";

            if(codeGraph.HasReturnedBody())
                builder.AppendLine($"result, err := {clientVarName}.{GetFluentApiPath(codeGraph.Nodes)}{methodName}({parametersList})");
            else
                builder.AppendLine($"{clientVarName}.{GetFluentApiPath(codeGraph.Nodes)}{methodName}({parametersList})");
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
                builder.AppendLine($"{indentManager.GetIndent()}{requestBodyVarName} := graphmodels.New{codeGraph.Body.Name}()");
                WriteCodePropertyObject(requestBodyVarName, builder, codeGraph.Body, indentManager);
            }
        }
        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
            else return string.Empty;
        }

        private static void WriteArrayProperty(string propertyAssignment, string objectName, StringBuilder builder, CodeProperty parentProperty, CodeProperty codeProperty, IndentManager indentManager){
            var contentBuilder = new StringBuilder();
            var propertyName = NormalizeJsonName(codeProperty.Name.ToFirstCharacterLowerCase());

            var objectBuilder = new StringBuilder();
            IndentManager indentManagerObjects = new IndentManager();

            indentManager.Indent();
            var childPosition = 0;
            foreach (var child in codeProperty.Children){
                if(child.PropertyType == PropertyType.Object){
                    WriteCodeProperty(propertyAssignment, objectBuilder, codeProperty, child , indentManagerObjects, childPosition);
                    contentBuilder.AppendLine($"{indentManager.GetIndent()}{child.Name.ToFirstCharacterLowerCase()?.IndexSuffix(childPosition)},");
                }else{
                    WriteCodeProperty(propertyAssignment, contentBuilder, codeProperty, child , indentManager, childPosition);
                }
                childPosition++;
            }
            indentManager.Unindent();

            if(objectBuilder.Length > 0){
                builder.AppendLine("\n");
                builder.AppendLine(objectBuilder.ToString());
            }
            
            var typeName = NativeTypes.Contains(codeProperty.TypeDefinition?.ToLower()?.Trim()) ? codeProperty.TypeDefinition : $"graphmodels.{codeProperty.TypeDefinition }able" ;
            builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := []{typeName} {{");
            builder.AppendLine(contentBuilder.ToString());
            builder.AppendLine($"{indentManager.GetIndent()}}}");
            if(parentProperty.PropertyType == PropertyType.Object)
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
                    var propName = codeProperty.PropertyType == PropertyType.Map ? $"\"{child.Name.ToFirstCharacterLowerCase()}\"" : NormalizeJsonName(child.Name.ToFirstCharacterLowerCase());
                    if (isArray || String.IsNullOrWhiteSpace(propName))
                        builder.AppendLine($"{indentManager.GetIndent()}\"{child.Value}\",");
                    else if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyName.AddQuotes()} : \"{child.Value}\", ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := \"{child.Value}\"");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Enum:
                    if (!String.IsNullOrWhiteSpace(child.Value))
                    {
                        var enumProperties = string.Join("_", child.Value.Split('.').Reverse().Select(x => x.ToUpper()));
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := graphmodels.{enumProperties} ");
                        builder.AppendLine($"{indentManager.GetIndent()}{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Date:
                    builder.AppendLine($"{propertyName} , err := time.Parse(time.RFC3339, \"{child.Value}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                case PropertyType.Int32:
                    if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : int32({child.Value}) , ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := int32({child.Value})");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Int64:
                    if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : int64({child.Value}) , ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := int64({child.Value})");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Float32:
                    if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : float32({child.Value}) , ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := float32({child.Value})");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Float64:
                    if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : float64({child.Value}) , ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := float64({child.Value})");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Double:
                    if (isMap)
                        builder.AppendLine($"{indentManager.GetIndent()}\"{propertyName}\" : float64({child.Value}) , ");
                    else
                    {
                        builder.AppendLine($"{propertyName} := float64({child.Value})");
                        builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    }
                    break;
                case PropertyType.Base64Url:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := []byte(\"{child.Value.ToFirstCharacterLowerCase()}\")");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
                default:
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := {child.Value.ToFirstCharacterLowerCase()}");
                    builder.AppendLine($"{propertyAssignment}.Set{propertyName.ToFirstCharacterUpperCase()}(&{propertyName}) ");
                    break;
            }
        }

        private static void WriteCodePropertyObject(string propertyAssignment, StringBuilder builder, CodeProperty codeProperty, IndentManager indentManager)
        {
            var childPosition = 0;
            foreach (var child in codeProperty.Children)
                WriteCodeProperty(propertyAssignment,builder,codeProperty,child,indentManager,childPosition++);
        }

        private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes) {
            if(!(nodes?.Any() ?? false)) return string.Empty;
            return nodes.Select(x => {
                                        if(x.Segment.IsCollectionIndex())
                                            return $"ById{x.Segment.Replace("{", "(\"").Replace("}", "\")")}.";
                                        else if (x.Segment.IsFunction()) {
                                            var parameters = x.PathItems[OpenApiSnippetsGenerator.treeNodeLabel]
                                                .Parameters
                                                .Where(y => y.In == ParameterLocation.Path)
                                                .Select(y => y.Name)
                                                .ToList();
                                            var paramSet = string.Join(", ", parameters);
                                            return x.Segment.Split('.').Last().ToFirstCharacterUpperCase() + $"({paramSet}).";
                                        }
                                        return x.Segment.ToFirstCharacterUpperCase() + "().";
                                    })
                        .Aggregate((x, y) => $"{x}{y}")
                        .Replace("().ById(", "ById(");
        }
    }
}
