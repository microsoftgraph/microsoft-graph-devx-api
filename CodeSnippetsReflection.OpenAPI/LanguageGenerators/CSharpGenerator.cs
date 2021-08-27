using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
	public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
	{
		private const string clientVarName = "graphClient";
		private const string clientVarType = "GraphClient";
		private const string httpCoreVarName = "httpCore";
		public string GenerateCodeSnippet(SnippetModel snippetModel)
		{
			var indentManager = new IndentManager();
			var snippetBuilder = new StringBuilder(
									"//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
									$"var {clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
			var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel, indentManager);
			snippetBuilder.Append(requestPayload);
			var responseAssignment = snippetModel.ResponseSchema == null ? string.Empty : "var result = ";
			snippetBuilder.AppendLine($"{responseAssignment}await {clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}.{GetMethodName(snippetModel.Method)}({payloadVarName});");
			return snippetBuilder.ToString();
		}
		private const string requestBodyVarName = "requestBody";
		private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel, IndentManager indentManager) {
			if(string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
				return (default, default);
			if(indentManager == null) throw new ArgumentNullException(nameof(indentManager));
			
			var payloadSB = new StringBuilder();
			switch (snippetModel.ContentType.Split(';').First().ToLowerInvariant()) {
				case "application/json":
					using (var ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(snippetModel.RequestBody))) {
						var parsedBody = JsonDocument.Parse(ms);
						var schema = snippetModel.RequestSchema;
						var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
						payloadSB.AppendLine($"var {requestBodyVarName} = new {className} {{");
						WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager);
						payloadSB.AppendLine("};");
					}
				break;
				case "application/octect-stream":
					payloadSB.AppendLine($"using var {requestBodyVarName} = new MemoryStream(); //stream to upload");
				break;
				default:
					throw new InvalidOperationException($"Unsupported content type: {snippetModel.ContentType}");
			}
			return (payloadSB.ToString(), requestBodyVarName);
		}
		private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager, bool includePropertyAssignment = true) {
			indentManager.Indent();
			foreach(var property in value.EnumerateObject()) {
				var propertyName = property.Name.ToFirstCharacterUpperCase();
				var propertyAssignment = includePropertyAssignment ? $"{indentManager.GetIndent()}{propertyName} = " : string.Empty;
				var propSchema = schema.GetPropertySchema(property.Name);
				WriteProperty(payloadSB, property.Value, propSchema, indentManager, propertyAssignment);
			}
			indentManager.Unindent();
		}
		private static void WriteProperty(StringBuilder payloadSB, JsonElement value, OpenApiSchema propSchema, IndentManager indentManager, string propertyAssignment) {
			switch (value.ValueKind) {
				case JsonValueKind.String:
					if(propSchema?.Format?.Equals("base64url", StringComparison.OrdinalIgnoreCase) ?? false)
						payloadSB.AppendLine($"{propertyAssignment}Encoding.ASCII.GetBytes(\"{value.GetString()}\"),");
					else
						payloadSB.AppendLine($"{propertyAssignment}\"{value.GetString()}\",");
					break;
				case JsonValueKind.Number:
					payloadSB.AppendLine($"{propertyAssignment}{GetNumberLiteral(propSchema, value)},");
					break;
				case JsonValueKind.False:
				case JsonValueKind.True:
					payloadSB.AppendLine($"{propertyAssignment}{value.GetBoolean().ToString().ToLowerInvariant()},");
					break;
				case JsonValueKind.Null:
					payloadSB.AppendLine($"{propertyAssignment}null,");
					break;
				case JsonValueKind.Object:
					if(propSchema != null) {
						payloadSB.AppendLine($"{propertyAssignment}new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()} {{");
						WriteJsonObjectValue(payloadSB, value, propSchema, indentManager);
						payloadSB.AppendLine($"{indentManager.GetIndent()}}},");
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
			var genericType = schema.GetSchemaTitle().ToFirstCharacterUpperCase() ?? value.EnumerateArray().First().ValueKind.ToString();
			payloadSB.AppendLine($"{propertyAssignment}new List<{genericType}> {{");
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
		private static string GetMethodName(HttpMethod method) {
			// can't use pattern matching with switch as it's not an enum but a bunch of static values
			if(method == HttpMethod.Get) return "GetAsync";
			else if(method == HttpMethod.Post) return "PostAsync";
			else if(method == HttpMethod.Put) return "PutAsync";
			else if(method == HttpMethod.Delete) return "DeleteAsync";
			else if(method == HttpMethod.Patch) return "PatchAsync";
			else if(method == HttpMethod.Head) return "HeadAsync";
			else if(method == HttpMethod.Options) return "OptionsAsync";
			else if(method == HttpMethod.Trace) return "TraceAsync";
			else throw new InvalidOperationException($"Unsupported HTTP method: {method}");
		}
	}
}