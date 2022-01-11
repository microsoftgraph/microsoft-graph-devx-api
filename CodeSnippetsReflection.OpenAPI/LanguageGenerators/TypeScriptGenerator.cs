﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        private const string clientVarName = "graphServiceClient";
        private const string clientVarType = "GraphServiceClient";
        private const string httpCoreVarName = "requestAdapter";

        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            if (snippetModel == null) throw new ArgumentNullException("Argument snippetModel cannot be null");
            var indentManager = new IndentManager();
            var snippetBuilder = new StringBuilder(
                                    "//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
                                    $"const {clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
            var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
            snippetBuilder.Append(requestPayload);
            var responseAssignment = snippetModel.ResponseSchema == null ? string.Empty : "const result = ";

            // add headers
            var (requestHeadersPayload, requestHeadersVarName) = GetRequestHeaders(snippetModel, indentManager);
            if (!string.IsNullOrEmpty(requestHeadersPayload)) snippetBuilder.Append(requestHeadersPayload);

            // add query parameters
            var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
            if (!string.IsNullOrEmpty(queryParamsPayload)) snippetBuilder.Append(queryParamsPayload);

            // add parameters
            var parametersList = GetActionParametersList(payloadVarName, queryParamsVarName, requestHeadersVarName);
            var methodName = snippetModel.Method.ToString().ToLower();
            snippetBuilder.AppendLine($"{responseAssignment}async () => {{");
            indentManager.Indent();
            snippetBuilder.AppendLine($"{indentManager.GetIndent()}await {clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}.{methodName}({parametersList});");
            indentManager.Unindent();
            snippetBuilder.AppendLine($"}}");

            return snippetBuilder.ToString();
        }
        private const string RequestHeadersVarName = "headers";
        private static (string, string) GetRequestHeaders(SnippetModel snippetModel, IndentManager indentManager)
        {
            var payloadSB = new StringBuilder();
            var filteredHeaders = snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                                                            .ToList();
            if (filteredHeaders.Any())
            {
                payloadSB.AppendLine($"{indentManager.GetIndent()}const {RequestHeadersVarName} = {{");
                indentManager.Indent();
                filteredHeaders.ForEach(h =>
                    payloadSB.AppendLine($"{indentManager.GetIndent()}\"{h.Key}\": \"{h.Value.FirstOrDefault().Replace("\"", "\\\"")}\",")
                );
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}};");
                return (payloadSB.ToString(), RequestHeadersVarName);
            }
            return (default, default);
        }
        private static string GetActionParametersList(params string[] parameters)
        {
            var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
            if (nonEmptyParameters.Any())
                return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
            else return string.Empty;
        }
        private const string RequestParametersVarName = "requestParameters";
        private static (string, string) GetRequestQueryParameters(SnippetModel model, IndentManager indentManager)
        {
            var payloadSB = new StringBuilder();
            if (!string.IsNullOrEmpty(model.QueryString))
            {
                payloadSB.AppendLine($"{indentManager.GetIndent()}let {RequestParametersVarName} = {{");
                indentManager.Indent();
                var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
                foreach (var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (queryParam.Contains("="))
                    {
                        var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        payloadSB.AppendLine($"{indentManager.GetIndent()}{NormalizeQueryParameterName(kvPair[0])} : {GetQueryParameterValue(kvPair[1], replacements)},");
                    }
                    else
                        payloadSB.AppendLine($"q.{indentManager.GetIndent()}{NormalizeQueryParameterName(queryParam)} = undefined;");
                }
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}}};");
                return (payloadSB.ToString(), RequestParametersVarName);
            }
            return (default, default);
        }
        private static Regex nestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase|RegexOptions.Compiled);
        private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams)
        {
            var replacements = new Dictionary<string, string>();
            var matches = nestedStatementRegex.Matches(queryParams);
            if (matches.Any())
                foreach (Match match in matches)
                {
                    var key = match.Groups[1].Value;
                    var value = match.Groups[2].Value;
                    replacements.Add(key, value);
                    queryParams = queryParams.Replace(value, string.Empty);
                }
            return (queryParams, replacements);
        }
        private static string GetQueryParameterValue(string originalValue, Dictionary<string, string> replacements)
        {
            if (originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                return originalValue.ToLowerInvariant();
            else if (int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            else
            {
                var valueWithNested = originalValue.Split(',')
                                                    .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v)
                                                    .Aggregate((a, b) => $"{a},{b}");
                return $"\"{valueWithNested}\"";
            }
        }
        private static string NormalizeQueryParameterName(string queryParam) => queryParam.TrimStart('$').ToFirstCharacterLowerCase();
        private const string RequestBodyVarName = "requestBody";
        private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager)
        {
            if (string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
                return (default, default);
            if (indentManager == null) throw new ArgumentNullException(nameof(indentManager));

            var payloadSB = new StringBuilder();
            switch (snippetModel.ContentType?.Split(';').First().ToLowerInvariant())
            {
                case "application/json":
                    TryParseBody(snippetModel, payloadSB, indentManager);
                    break;
                case "application/octet-stream":
                    payloadSB.AppendLine($"using var {RequestBodyVarName} = new WebStream();");
                    break;
                default:
                    if(TryParseBody(snippetModel, payloadSB, indentManager)) //in case the content type header is missing but we still have a json payload
                        break;
                    else
					    throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
            }
            var result = payloadSB.ToString();
			return (result, string.IsNullOrEmpty(result) ? string.Empty : RequestBodyVarName);
        }
        private static bool TryParseBody(SnippetModel snippetModel, StringBuilder payloadSB, IndentManager indentManager) {
            if(snippetModel.IsRequestBodyValid)
                try {
                    using var parsedBody = JsonDocument.Parse(snippetModel.RequestBody, new JsonDocumentOptions { AllowTrailingCommas = true });
                    var schema = snippetModel.RequestSchema;
                    var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
                    payloadSB.AppendLine($"const {RequestBodyVarName} = new {className}()");
                    WriteJsonObjectValue(RequestBodyVarName, payloadSB, parsedBody.RootElement, schema, indentManager);
                    return true;
                } catch (Exception ex) when (ex is JsonException || ex is ArgumentException) {
                    // the payload wasn't json or poorly formatted
                }
            return false;
        }
        private static void WriteAnonymousObjectValues(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();

            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema)
            {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterLowerCase();
                var propertyAssignment = includePropertyAssignment ? $"{indentManager.GetIndent()} [\"{propertyName}\" , " : string.Empty;
                WriteProperty(string.Empty, payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment, "]", ",");
            }

            indentManager.Unindent();
        }
        private static void WriteJsonObjectValue(String objectName, StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();
            var propertiesAndSchema = value.EnumerateObject()
                                            .Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
            foreach (var propertyAndSchema in propertiesAndSchema.Where(x => x.Item2 != null))
            {
                var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterLowerCase();
                var propertyAssignment = includePropertyAssignment ? $"{objectName}.{propertyName} = " : string.Empty;
                WriteProperty($"{objectName}.{propertyName}", payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment);
            }
            var propertiesWithoutSchema = propertiesAndSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
            if (propertiesWithoutSchema.Any())
            {
                payloadSB.AppendLine($"{objectName}.additionalData = new Map([");
                indentManager.Indent();
                foreach (var property in propertiesWithoutSchema)
                {
                    var propertyAssignment = $"{indentManager.GetIndent()}[\"{property.Name}\", ";
                    WriteProperty(objectName, payloadSB, property.Value, null, indentManager, propertyAssignment, "]");
                }
                indentManager.Unindent();
                payloadSB.AppendLine($"{indentManager.GetIndent()}]);");
            }
            indentManager.Unindent();
        }
        private static void WriteProperty(String objectName, StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string propertySuffix = default, string terminateLine = ";")
        {
            switch (value.ValueKind)
            {
                case JsonValueKind.String:
                    if (propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment}btoa(\"{value.GetString()}\"){propertySuffix}{terminateLine}");
                    else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
                        payloadSB.AppendLine($"{propertyAssignment} new Date(\"{value.GetString()}\"){propertySuffix}{terminateLine}");
                    else
                        payloadSB.AppendLine($"{propertyAssignment}\"{value.GetString()}\"{propertySuffix}{terminateLine}");
                    break;
                case JsonValueKind.Number:
                    payloadSB.AppendLine($"{propertyAssignment}{value}{propertySuffix}{terminateLine}");
                    break;
                case JsonValueKind.False:
                case JsonValueKind.True:
                    payloadSB.AppendLine($"{propertyAssignment}{value.GetBoolean().ToString().ToLowerInvariant()}{propertySuffix}{terminateLine}");
                    break;
                case JsonValueKind.Null:
                    payloadSB.AppendLine($"{propertyAssignment}null{propertySuffix},");
                    break;
                case JsonValueKind.Object:
                    if (propSchema != null)
                    {
                        payloadSB.AppendLine($"{propertyAssignment}new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()}(){terminateLine}");
                        WriteJsonObjectValue(objectName, payloadSB, value, propSchema, indentManager);
                    }
                    else
                    {
                        WriteAnonymousObjectValues(payloadSB, value, propSchema, indentManager);
                    }
                    break;
                case JsonValueKind.Array:
                    WriteJsonArrayValue(objectName, payloadSB, value, propSchema, indentManager, propertyAssignment, "],");
                    break;
                default:
                    throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
            }
        }
        private static void WriteJsonArrayValue(String objectName, StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, string propertyAssignment, string terminateLine)
        {
            payloadSB.AppendLine($"{propertyAssignment}[");
            indentManager.Indent();
            indentManager.Indent();
            foreach (var item in value.EnumerateArray())
                WriteProperty(objectName, payloadSB, item, schema, indentManager, indentManager.GetIndent());
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}]");
            indentManager.Unindent();
            payloadSB.AppendLine($"{indentManager.GetIndent()}{terminateLine}");
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
