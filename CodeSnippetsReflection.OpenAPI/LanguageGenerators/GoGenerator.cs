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

        public string GenerateCodeSnippetNew(SnippetModel snippetModel)
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
            var childPosition = 1;
            foreach (var child in codeProperty.Children){
                if(child.PropertyType == PropertyType.Object){
                    WriteCodeProperty(propertyAssignment, objectBuilder, codeProperty, child , indentManagerObjects, childPosition);
                    contentBuilder.AppendLine($"{indentManager.GetIndent()}{child.Name.ToFirstCharacterLowerCase()}{(childPosition > 1 ? childPosition : null)},");
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
            builder.AppendLine($"{indentManager.GetIndent()}{propertyName} := [] {typeName} {{");
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
            var objectName = isArray ? $"{propertyName}{(childPosition > 1 ? childPosition : null)}" : propertyName; // an indexed object name for  objects in an array
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
                    builder.AppendLine($"{propertyName} , err  := time.Parse(time.RFC3339, \"{child.Value}\")");
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
                    builder.AppendLine($"{indentManager.GetIndent()}{NormalizeJsonName(child.Name.ToFirstCharacterLowerCase())} := \"{child.Value.ToFirstCharacterLowerCase()}\"");
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
            var childPosition = 1;
            foreach (var child in codeProperty.Children)
            {
                WriteCodeProperty(propertyAssignment,builder,codeProperty,child,indentManager,childPosition);
                childPosition++;
            }
        }

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder(
                                    "//THE GO SDK IS IN PREVIEW. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                                    $"{clientVarName} := msgraphsdk.New{clientVarType}({httpCoreVarName}){Environment.NewLine}{Environment.NewLine}");
            var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
            if(!string.IsNullOrEmpty(requestPayload))
                snippetBuilder.Append(requestPayload);
            var responseAssignment = "result, err := ";
            // have a return type if we have a response schema that is not an error
            if (snippetModel.ResponseSchema == null || (snippetModel.ResponseSchema.Properties.Count == 1 && snippetModel.ResponseSchema.Properties.First().Key.Equals("error", StringComparison.OrdinalIgnoreCase)))
                responseAssignment = string.Empty;
            var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
            if(!string.IsNullOrEmpty(queryParamsPayload))
                snippetBuilder.Append(queryParamsPayload);
            var (requestHeadersPayload, requestHeadersVarName) = GetRequestHeaders(snippetModel, indentManager);
            if(!string.IsNullOrEmpty(requestHeadersPayload))
                snippetBuilder.Append(requestHeadersPayload);
            var (configPayload, configVarName) = GetConfigurationParameter(snippetModel, indentManager, queryParamsVarName, requestHeadersVarName);
            if(!string.IsNullOrEmpty(configPayload))
                snippetBuilder.Append(configPayload);
            var pathParametersDeclaration = GetFluentApiPathVariablesDeclaration(snippetModel.PathNodes);
            pathParametersDeclaration.ToList().ForEach(x => snippetBuilder.AppendLine(x));
            var methodName = snippetModel.Method.ToString().ToLower().ToFirstCharacterUpperCase();
            if(!string.IsNullOrEmpty(configPayload))
            {
                methodName += $"WithRequestConfigurationAndResponseHandler";
                configVarName += ", nil";
            }
            var argumentSeparation = !string.IsNullOrEmpty(requestPayload) && !string.IsNullOrEmpty(configPayload) ? ", " : string.Empty;
            snippetBuilder.AppendLine($"{responseAssignment}{clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}{methodName}({payloadVarName}{argumentSeparation}{configVarName})");

            snippetBuilder.Append("\n\n =============================================================================== \n ////New Generator \n\n ");
            snippetBuilder.Append(GenerateCodeSnippetNew(snippetModel));
            return snippetBuilder.ToString();
        }

        private static (string, string) GetRequestHeaders(SnippetModel snippetModel, IndentManager indentManager) {
            var payloadSB = new StringBuilder();
            var filteredHeaders = snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                                                            .ToList();
            if(filteredHeaders.Any()) {
                payloadSB.AppendLine($"{indentManager.GetIndent()}{requestHeadersVarName} := map[string]string{{");
                indentManager.Indent();
                filteredHeaders.ForEach(h =>
                    payloadSB.AppendLine($"{indentManager.GetIndent()}\"{h.Key}\": \"{h.Value.FirstOrDefault()}\"")
                );
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
                return (payloadSB.ToString(), requestHeadersVarName);
            }
            return (default, default);
        }

        private static (string, string) GetConfigurationParameter(SnippetModel model, IndentManager indentManager, string queryParamsParam, string headersParam) {
            var nonEmptyParameters = new string[] { queryParamsParam, headersParam}.Where(p => !string.IsNullOrEmpty(p));
            if(nonEmptyParameters.Any()) {
                var className = $"msgraphsdk.{model.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{model.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}RequestConfiguration";
                var payloadSB = new StringBuilder();
                payloadSB.AppendLine($"{indentManager.GetIndent()}{optionsParameterVarName} := &{className}{{");
                indentManager.Indent();
                if(!string.IsNullOrEmpty(queryParamsParam))
                    payloadSB.AppendLine($"{indentManager.GetIndent()}QueryParameters: {queryParamsParam},");
                if(!string.IsNullOrEmpty(headersParam))
                    payloadSB.AppendLine($"{indentManager.GetIndent()}Headers: {headersParam},");
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
                return (payloadSB.ToString(), optionsParameterVarName);
            } else return (string.Empty, string.Empty);
        }

        private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager) {
            var payloadSB = new StringBuilder();
            if(!string.IsNullOrEmpty(model.QueryString)) {
                var className = $"msgraphsdk.{model.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{model.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}QueryParameters";
                payloadSB.AppendLine($"{indentManager.GetIndent()}{requestParametersVarName} := &{className}{{");
                indentManager.Indent();
                var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
                foreach(var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
                    if(queryParam.Contains("=")) {
                        var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        payloadSB.AppendLine($"{indentManager.GetIndent()}{NormalizeQueryParameterName(kvPair[0])}: {GetQueryParameterValue(kvPair[1], replacements)},");
                    } else
                        payloadSB.AppendLine($"{indentManager.GetIndent()}{NormalizeQueryParameterName(queryParam)}: \"\",");
                }
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
                return (payloadSB.ToString(), requestParametersVarName);
            }
            return (default, default);
        }
        private static Regex nestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams) {
            var replacements = new Dictionary<string, string>();
            var matches = nestedStatementRegex.Matches(queryParams);
            if(matches.Any())
                foreach(GroupCollection groupCollection in matches.Select(x => x.Groups)) {
                    var key = groupCollection[1].Value;
                    var value = groupCollection[2].Value;
                    if(value.Contains("=") && replacements.TryAdd(key, value)) // otherwise it might be a function call
                        queryParams = queryParams.Replace(value, string.Empty);
                }
            return (queryParams, replacements);
        }
        private static string GetQueryParameterValue(string originalValue, Dictionary<string, string> replacements) {
            if(originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                return originalValue.ToLowerInvariant();
            else if(int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            else {
                var valueWithNested = originalValue.Split(',')
                                                    .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                                                    .Aggregate((a, b) => $"{a},{b}");
                return $"\"{valueWithNested}\"";
            }
        }
        private static string NormalizeQueryParameterName(string queryParam) => queryParam.TrimStart('$').ToFirstCharacterUpperCase();
        private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager) {
            if(string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
                return (default, default);
            if(indentManager == null) throw new ArgumentNullException(nameof(indentManager));

            var payloadSB = new StringBuilder();
            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant()) {
                case "application/json":
                    TryParseBody(snippetModel, payloadSB, indentManager);
                break;
                case "application/octet-stream":
                    payloadSB.AppendLine($"{requestBodyVarName} := make([]byte, 0); //binary array to upload");
                break;
                default:
                    if(TryParseBody(snippetModel, payloadSB, indentManager)) //in case the content type header is missing but we still have a json payload
                        break;
                    else
                        throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
            }
            var result = payloadSB.ToString();
            return (result, string.IsNullOrEmpty(result) ? string.Empty : requestBodyVarName);
        }
        private static bool TryParseBody(SnippetModel snippetModel, StringBuilder payloadSB, IndentManager indentManager) {
            if(snippetModel.IsRequestBodyValid)
                try {
                    using var parsedBody = JsonDocument.Parse(snippetModel.RequestBody, new JsonDocumentOptions { AllowTrailingCommas = true });
                    var schema = snippetModel.RequestSchema;
                    var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
                    if (string.IsNullOrEmpty(className) &&
                        schema != null &&
                        schema.Properties.Any() &&
                        schema.Properties.Count == 1)
                        className = $"{schema.Properties.First().Key.ToFirstCharacterUpperCase()}RequestBody"; // edge case for odata actions with a single parameter
                    payloadSB.AppendLine($"{requestBodyVarName} := msgraphsdk.New{className}()");
                    WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager, variableName: requestBodyVarName);
                    return true;
                } catch (Exception ex) when (ex is JsonException || ex is ArgumentException) {
                    // the payload wasn't json or poorly formatted
                }
            return false;
        }
        private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true, string variableName = default) {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            if(!string.IsNullOrEmpty(variableName))
                variableName += ".";
            foreach(var propertyAndSchema in propertiesAndSchema.Where(x => x.Item2 != null)) {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
                var propertyAssignment = includePropertyAssignment ? $"{indentManager.GetIndent()}{variableName}Set{propertyName}(" : string.Empty;
                WriteProperty(payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment, propertyName.ToFirstCharacterLowerCase(), ")");
            }
            var propertiesWithoutSchema = propertiesAndSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
            if(propertiesWithoutSchema.Any()) {
                payloadSB.AppendLine($"{indentManager.GetIndent()}{variableName}SetAdditionalData(map[string]interface{{}}{{");
                indentManager.Indent();
                foreach(var property in propertiesWithoutSchema) {
                    var propertyAssignment = $"{indentManager.GetIndent()}\"{property.Name}\": ";
                    WriteProperty(payloadSB, property.Value, null, indentManager, propertyAssignment, null, ",");
                }
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
            }
        }
        private static void WritePropertyFromTempVar(StringBuilder payloadSB, string propertyAssignment, string tempVarName, string valueAssignment, string valueDeclaration, bool addPointer, string propertySuffix) {
            if(!string.IsNullOrEmpty(tempVarName)) {
                payloadSB.AppendLine($"{tempVarName}{valueAssignment}{valueDeclaration}");
                var pointer = addPointer ? "&" : string.Empty;
                payloadSB.AppendLine($"{propertyAssignment}{pointer}{tempVarName}{propertySuffix}");
            } else {
                payloadSB.AppendLine($"{propertyAssignment}{valueDeclaration}{propertySuffix}");
            }
        }
        private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string tempVarName, string propertySuffix = default) {
            switch (value.ValueKind) {
                case JsonValueKind.String:
                    if(propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
                        WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", $"[]byte(\"{value.GetString()}\")", true, propertySuffix);
                    else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
                        WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, ", err := ", $"time.Parse(time.RFC3339, \"{value.GetString()}\")", true, propertySuffix);
                    else if (propSchema?.Format?.Equals("guid", StringComparison.OrdinalIgnoreCase) ?? false)
                        WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", $"uuid.MustParse(\"{value.GetString()}\")", true, propertySuffix);
                    else
                        WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", $"\"{value.GetString()}\"", true, propertySuffix);
                    break;
                case JsonValueKind.Number:
                    WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", GetNumberLiteral(propSchema, value), true, propertySuffix);
                    break;
                case JsonValueKind.False:
                case JsonValueKind.True:
                    WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", value.GetBoolean().ToString().ToLowerInvariant(), true, propertySuffix);
                    break;
                case JsonValueKind.Null:
                    payloadSB.AppendLine($"{propertyAssignment}nil{propertySuffix}");
                    break;
                case JsonValueKind.Object:
                    if(propSchema != null) {
                        WritePropertyFromTempVar(payloadSB, propertyAssignment, tempVarName, " := ", $"msgraphsdk.New{propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()}()", false, propertySuffix);
                        WriteJsonObjectValue(payloadSB, value, propSchema, indentManager, variableName: tempVarName);
                    }
                    break;
                case JsonValueKind.Array:
                    WriteJsonArrayValue(payloadSB, value, propSchema, indentManager, propertyAssignment);
                break;
                default:
                    throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
            }
        }
        private static void WriteJsonArrayValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, string propertyAssignment) {
            var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ??
                            (value.EnumerateArray().Any() ?
                                value.EnumerateArray().First().ValueKind.ToString() :
                                schema.Items?.Type); // it's an empty array of primitives
            payloadSB.AppendLine($"{propertyAssignment} []{genericType} {{");
            indentManager.Indent();
            foreach(var item in value.EnumerateArray())
                WriteProperty(payloadSB, item, schema, indentManager, indentManager.GetIndent(), null, ",");
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
        }
        private static string GetNumberLiteral(OpenApiSchema schema, JsonElement value) {
            if(schema == default) return default;
            return schema.Type switch {
                "integer" when schema.Format.Equals("int32") => $"int32({value.GetInt32()})",
                "integer" when schema.Format.Equals("int64") => $"int64({value.GetInt64()})",
                _ when schema.Format.Equals("float") || schema.Format.Equals("float32") => $"float32({value.GetDecimal()})",
                _ when schema.Format.Equals("float64") => $"float64({value.GetDecimal()})",
                _ when schema.Format.Equals("double") => $"float64({value.GetDouble()})", //in MS Graph float & double are any of number, string and enum
                _ => value.GetInt32().ToString(),
            };
        }
        private static IEnumerable<string> GetFluentApiPathVariablesDeclaration(IEnumerable<OpenApiUrlTreeNode> nodes) {
            return nodes.Where(x => x.Segment.IsCollectionIndex())
                    .Select(x => x.Segment.TrimStart('{').TrimEnd('}'))
                    .Select(x => $"{idCleanupRegex.Replace(x, m => m.Groups[1].Value.ToFirstCharacterUpperCase())} := \"{x}\"");
        }
        private static Regex idCleanupRegex = new Regex(@"-(\w)", RegexOptions.Compiled);
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
