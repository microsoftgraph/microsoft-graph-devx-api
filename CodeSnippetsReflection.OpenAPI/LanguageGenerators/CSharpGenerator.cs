using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
	public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
	{
		private const string clientVarName = "graphClient";
		private const string clientVarType = "GraphServiceClient";
		private const string httpCoreVarName = "requestAdapter";
		public string GenerateCodeSnippet(SnippetModel snippetModel)
		{
			var indentManager = new IndentManager();
			var snippetBuilder = new StringBuilder(
									"//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
									$"var {clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
			var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
			snippetBuilder.Append(requestPayload);
            var responseAssignment = "var result = ";
            // have a return type if we have a response schema that is not an error
            if (snippetModel.ResponseSchema == null || (snippetModel.ResponseSchema.Properties.Count == 1 && snippetModel.ResponseSchema.Properties.First().Key.Equals("error",StringComparison.OrdinalIgnoreCase)))
                responseAssignment = string.Empty;

            var requestConfigurationPayload = GetRequestConfiguration(snippetModel, indentManager);
            
            var parametersList = GetActionParametersList(payloadVarName,requestConfigurationPayload);
            var methodName = snippetModel.Method.ToString().ToLower().ToFirstCharacterUpperCase() + "Async";
			snippetBuilder.AppendLine($"{responseAssignment}await {clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}.{methodName}({parametersList});");
			return snippetBuilder.ToString();
		}
		private const string requestHeadersPropertyName = "Headers";
		private const string requestConfigurationVarName = "requestConfiguration";

        private static string GetRequestConfiguration(SnippetModel snippetModel, IndentManager indentManager)
        {
            var payloadSB = new StringBuilder();
            var queryParamsPayload = GetRequestQueryParameters(snippetModel, indentManager);
            var requestHeadersPayload = GetRequestHeaders(snippetModel, indentManager);

            if (!string.IsNullOrEmpty(queryParamsPayload) || !string.IsNullOrEmpty(requestHeadersPayload))
            {
                payloadSB.AppendLine($"({requestConfigurationVarName}) =>");
                payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
                payloadSB.Append(queryParamsPayload);
                payloadSB.Append(requestHeadersPayload);
                payloadSB.Append($"{indentManager.GetIndent()}}}");
            }
            
            return payloadSB.Length > 0 ? payloadSB.ToString() : default;
        }

        private static string GetRequestHeaders(SnippetModel snippetModel, IndentManager indentManager) {
			var payloadSB = new StringBuilder();
			var filteredHeaders = snippetModel.RequestHeaders.Where(h => !h.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
															.ToList();
			if(filteredHeaders.Any()) {
				indentManager.Indent();
				filteredHeaders.ForEach(h =>
					payloadSB.AppendLine($"{indentManager.GetIndent()}{requestConfigurationVarName}.{requestHeadersPropertyName}.Add(\"{h.Key}\", \"{h.Value.FirstOrDefault()}\");")
				);
				indentManager.Unindent();
				return payloadSB.ToString();
			}
			return default;
		}
		private static string GetActionParametersList(params string[] parameters) {
			var nonEmptyParameters = parameters.Where(p => !string.IsNullOrEmpty(p));
			if(nonEmptyParameters.Any())
				return string.Join(", ", nonEmptyParameters.Aggregate((a, b) => $"{a}, {b}"));
			else return string.Empty;
		}
		private const string requestParametersPropertyName = "QueryParameters";
		private static string GetRequestQueryParameters(SnippetModel model, IndentManager indentManager) {
			var payloadSB = new StringBuilder();
			if(!string.IsNullOrEmpty(model.QueryString)) {
				indentManager.Indent();
				var (queryString, replacements) = ReplaceNestedOdataQueryParameters(model.QueryString);
				foreach(var queryParam in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)) {
					if(queryParam.Contains('=')) {
						var kvPair = queryParam.Split('=', StringSplitOptions.RemoveEmptyEntries);
                        var isCollection = kvPair[0].Contains("select") || kvPair[0].Contains("expand") || kvPair[0].Contains("orderby");
						payloadSB.AppendLine($"{indentManager.GetIndent()}{requestConfigurationVarName}.{requestParametersPropertyName}.{NormalizeQueryParameterName(kvPair[0])} = {GetQueryParameterValue(kvPair[1], replacements, isCollection)};");
					} else
						payloadSB.AppendLine($"{indentManager.GetIndent()}{requestConfigurationVarName}.{requestParametersPropertyName}.{NormalizeQueryParameterName(queryParam)} = string.Empty;");
				}
				indentManager.Unindent();
				return (payloadSB.ToString());
			}
			return default;
		}
		private static readonly Regex nestedStatementRegex = new(@"(\w+)(\([^)]+\))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static (string, Dictionary<string, string>) ReplaceNestedOdataQueryParameters(string queryParams) {
			var replacements = new Dictionary<string, string>();
			var matches = nestedStatementRegex.Matches(queryParams);
			if(matches.Any())
				foreach(Match match in matches) {
					var key = match.Groups[1].Value;
					var value = match.Groups[2].Value;
					replacements.Add(key, value);
					queryParams = queryParams.Replace(value, string.Empty);
				}
			return (queryParams, replacements);
		}
		private static string GetQueryParameterValue(string originalValue, Dictionary<string, string> replacements,bool isCollection)
        {
            // boolean - true or false
            if(originalValue.Equals("true", StringComparison.OrdinalIgnoreCase) || originalValue.Equals("false", StringComparison.OrdinalIgnoreCase))
				return originalValue.ToLowerInvariant();
            // try to parse as number
            if(int.TryParse(originalValue, out var intValue))
                return intValue.ToString();
            // its a string value with more info inside
            var valueWithNested = originalValue.Split(',')
                .Select(v => replacements.ContainsKey(v) ? v + replacements[v] : v) // rejoin nested query parameter if present
                .Select(a => $"\"{a}\"") // surround the string with double quotes
                .Aggregate((a, b) => $"{a} , {b}"); // multiple elements are comma separated

            return isCollection ? $"new [] {{ {valueWithNested} }}" : valueWithNested;
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
					payloadSB.AppendLine($"using var {RequestBodyVarName} = new MemoryStream(); //stream to upload");
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
                    if (string.IsNullOrEmpty(className) && schema != null && schema.Properties.Count == 1)
                        className = $"{schema.Properties.First().Key.ToFirstCharacterUpperCase()}RequestBody"; // edge case for odata actions with a single parameter
                    if (string.IsNullOrEmpty(className))
                        className = $"{snippetModel.ResponseVariableName.ToFirstCharacterUpperCase()}RequestBody"; // default to the cleaned up url path node as class name
                    payloadSB.AppendLine($"var {RequestBodyVarName} = new {className}");
                    payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
                    WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager);
                    payloadSB.AppendLine("};");

                } catch (Exception ex) when (ex is JsonException || ex is ArgumentException) {
                    // the payload wasn't json or poorly formatted
                }
            return false;
        }
		private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true) {
			if (value.ValueKind != JsonValueKind.Object) throw new InvalidOperationException($"Expected JSON object and got {value.ValueKind}");
            indentManager.Indent();
			var propertiesAndSchema = value.EnumerateObject()
											.Select(x => new Tuple<JsonProperty, OpenApiSchema>(x, schema.GetPropertySchema(x.Name)));
			foreach(var propertyAndSchema in propertiesAndSchema.Where(x => x.Item2 != null)) {
				var propertyName = propertyAndSchema.Item1.Name.ToFirstCharacterUpperCase();
				var propertyAssignment = includePropertyAssignment ? $"{indentManager.GetIndent()}{propertyName} = " : string.Empty;
				WriteProperty(payloadSB, propertyAndSchema.Item1.Value, propertyAndSchema.Item2, indentManager, propertyAssignment);
			}
			var propertiesWithoutSchema = propertiesAndSchema.Where(x => x.Item2 == null).Select(x => x.Item1);
			if(propertiesWithoutSchema.Any()) {
				payloadSB.AppendLine($"{indentManager.GetIndent()}AdditionalData = new()");
                payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
				indentManager.Indent();
				foreach(var property in propertiesWithoutSchema) {
					var propertyAssignment = $"{indentManager.GetIndent()}{{\"{property.Name}\", ";
					WriteProperty(payloadSB, property.Value, null, indentManager, propertyAssignment , "}");
				}
				indentManager.Unindent();
				payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
			}
			indentManager.Unindent();
		}
		private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment, string propertySuffix = default) {
			switch (value.ValueKind) {
				case JsonValueKind.String:
					if(propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
						payloadSB.AppendLine($"{propertyAssignment}Convert.FromBase64String(\"{value.GetString()}\"){propertySuffix},");
					else if (propSchema?.Format?.Equals("date-time", StringComparison.OrdinalIgnoreCase) ?? false)
						payloadSB.AppendLine($"{propertyAssignment}DateTimeOffset.Parse(\"{value.GetString()}\"){propertySuffix},");
					else
						payloadSB.AppendLine($"{propertyAssignment}\"{value.GetString()}\"{propertySuffix},");
					break;
				case JsonValueKind.Number:
					payloadSB.AppendLine($"{propertyAssignment}{GetNumberLiteral(propSchema, value)}{propertySuffix},");
					break;
				case JsonValueKind.False:
				case JsonValueKind.True:
					payloadSB.AppendLine($"{propertyAssignment}{value.GetBoolean().ToString().ToLowerInvariant()}{propertySuffix},");
					break;
				case JsonValueKind.Null:
					payloadSB.AppendLine($"{propertyAssignment}null{propertySuffix},");
					break;
				case JsonValueKind.Object:
					if(propSchema != null) {
						payloadSB.AppendLine($"{propertyAssignment}new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()}");
                        payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
						WriteJsonObjectValue(payloadSB, value, propSchema, indentManager);
						payloadSB.AppendLine($"{indentManager.GetIndent()}}}{propertySuffix},");
					}
					break;
				case JsonValueKind.Array:
					WriteJsonArrayValue(payloadSB, value, propSchema, indentManager, propertyAssignment);
				break;
                case JsonValueKind.Undefined:
                default:
					throw new NotImplementedException($"Unsupported JsonValueKind: {value.ValueKind}");
			}
		}
		private static void WriteJsonArrayValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, string propertyAssignment) {
			var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? value.EnumerateArray().First().ValueKind.ToString();
			payloadSB.AppendLine($"{propertyAssignment}new List<{genericType}>");
            payloadSB.AppendLine($"{indentManager.GetIndent()}{{");
			indentManager.Indent();
			foreach(var item in value.EnumerateArray())
				WriteProperty(payloadSB, item, schema, indentManager, indentManager.GetIndent());
			indentManager.Unindent();
			payloadSB.AppendLine($"{indentManager.GetIndent()}}}");
		}
		private static string GetNumberLiteral(OpenApiSchema schema, JsonElement value) {
			if(schema == default) return default;
			return schema.Type switch {
				"integer" when schema.Format.Equals("int64") => $"{value.GetInt64()}L",
				_ when schema.Format.Equals("float") => $"{value.GetDecimal()}f",
				_ when schema.Format.Equals("double") => $"{value.GetDouble()}d", //in MS Graph float & double are any of number, string and enum
				_ => value.GetInt32().ToString(),
			};
		}
		private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes) {
			if(!(nodes?.Any() ?? false)) return string.Empty;
			return nodes.Select(x => {
										if(x.Segment.IsCollectionIndex())
											return x.Segment.Replace("{", "[\"").Replace("}", "\"]");
										else if (x.Segment.IsFunction())
											return x.Segment.Split('.').Last().ToFirstCharacterUpperCase();
										return x.Segment.ToFirstCharacterUpperCase();
									})
						.Aggregate((x, y) => {
							var dot = y.StartsWith("[") ?
											string.Empty :
											".";
							return $"{x}{dot}{y}";
						});
		}
	}
}
