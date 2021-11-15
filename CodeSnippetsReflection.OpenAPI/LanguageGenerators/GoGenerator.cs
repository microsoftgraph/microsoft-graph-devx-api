using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
    public class GoGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
    {
        private const string clientVarName = "graphClient";
		private const string clientVarType = "GraphServiceClient";
		private const string httpCoreVarName = "requestAdapter";
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            var indentManager = new IndentManager();
			var snippetBuilder = new StringBuilder(
									"//THE GO SDK IS IN PREVIEW. NON-PRODUCTION USE ONLY" + Environment.NewLine +
									$"{clientVarName} := msgraphsdk.New{clientVarType}({httpCoreVarName}){Environment.NewLine}{Environment.NewLine}");
			var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
			snippetBuilder.Append(requestPayload);
			var responseAssignment = snippetModel.ResponseSchema == null ? string.Empty : "result, err := ";
			var (queryParamsPayload, queryParamsVarName) = GetRequestQueryParameters(snippetModel, indentManager);
			if(!string.IsNullOrEmpty(queryParamsPayload))
				snippetBuilder.Append(queryParamsPayload);
			var (requestHeadersPayload, requestHeadersVarName) = GetRequestHeaders(snippetModel, indentManager);
			if(!string.IsNullOrEmpty(requestHeadersPayload))
				snippetBuilder.Append(requestHeadersPayload);
			var (optionsPayload, optionsVarName) = GetOptionsParameter(snippetModel, indentManager, payloadVarName, queryParamsVarName, requestHeadersVarName);
            if(!string.IsNullOrEmpty(optionsPayload))
                snippetBuilder.Append(optionsPayload);
            var pathParametersDeclaration = GetFluentApiPathVariablesDeclaration(snippetModel.PathNodes);
            pathParametersDeclaration.ToList().ForEach(x => snippetBuilder.AppendLine(x));
			snippetBuilder.AppendLine($"{responseAssignment}{clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}{GetMethodName(snippetModel.Method)}({optionsParameterVarName})");
			return snippetBuilder.ToString();
        }
		private const string requestHeadersVarName = "headers";
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
        private const string optionsParameterVarName = "options";
		private static (string, string) GetOptionsParameter(SnippetModel model, IndentManager indentManager, string payloadParam, string queryParamsParam, string headersParam) {
			var nonEmptyParameters = new string[] { payloadParam, queryParamsParam, headersParam}.Where(p => !string.IsNullOrEmpty(p));
			if(nonEmptyParameters.Any()) {
                var className = $"msgraphsdk.{model.PathNodes.Last().GetClassName("RequestBuilder").ToFirstCharacterUpperCase()}{model.Method.ToString().ToLowerInvariant().ToFirstCharacterUpperCase()}Options";
    			var payloadSB = new StringBuilder();
				payloadSB.AppendLine($"{indentManager.GetIndent()}{optionsParameterVarName} := &{className}{{");
                indentManager.Indent();
                if(!string.IsNullOrEmpty(payloadParam))
                    payloadSB.AppendLine($"{indentManager.GetIndent()}Body: {payloadParam},");
                if(!string.IsNullOrEmpty(queryParamsParam))
                    payloadSB.AppendLine($"{indentManager.GetIndent()}Q: {queryParamsParam},");
                if(!string.IsNullOrEmpty(headersParam))
                    payloadSB.AppendLine($"{indentManager.GetIndent()}H: {headersParam},");
                indentManager.Unindent();
				payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
				return (payloadSB.ToString(), optionsParameterVarName);
            } else return (string.Empty, "nil");
		}
		private const string requestParametersVarName = "requestParameters";
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
		private const string RequestBodyVarName = "requestBody";
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
					payloadSB.AppendLine($"{RequestBodyVarName} := make([]byte, 0); //binary array to upload");
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
            if(!string.IsNullOrEmpty(snippetModel.RequestBody) &&
                !"undefined".Equals(snippetModel.RequestBody, StringComparison.OrdinalIgnoreCase)) // graph explorer sends "undefined" as request body for some reason
                try {
                    using var parsedBody = JsonDocument.Parse(snippetModel.RequestBody);
                    var schema = snippetModel.RequestSchema;
                    var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
                    payloadSB.AppendLine($"{RequestBodyVarName} := msgraphsdk.New{className}()");
                    WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager, variableName: RequestBodyVarName);
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
											return idCleanupRegex.Replace(x.Segment.Replace("{", "ById(&").Replace("}", ")"), m => m.Groups[1].Value.ToFirstCharacterUpperCase()) + ".";
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
		private static string GetMethodName(HttpMethod method) {
			// can't use pattern matching with switch as it's not an enum but a bunch of static values
			if(method == HttpMethod.Get) return "Get";
			else if(method == HttpMethod.Post) return "Post";
			else if(method == HttpMethod.Put) return "Put";
			else if(method == HttpMethod.Delete) return "Delete";
			else if(method == HttpMethod.Patch) return "Patch";
			else if(method == HttpMethod.Head) return "Head";
			else if(method == HttpMethod.Options) return "Options";
			else if(method == HttpMethod.Trace) return "Trace";
			else throw new InvalidOperationException($"Unsupported HTTP method: {method}");
		}
    }
}
