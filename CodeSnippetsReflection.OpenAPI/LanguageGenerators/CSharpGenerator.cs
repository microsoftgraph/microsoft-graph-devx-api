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
			
			using var ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(snippetModel.RequestBody));
			var parsedBody = JsonDocument.Parse(ms);
			var schema = snippetModel.RequestSchema;
			var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
			var payloadSB = new StringBuilder($"var {requestBodyVarName} = new {className} {{{Environment.NewLine}");
			WriteJsonObjectValue(payloadSB, parsedBody.RootElement, schema, indentManager);
			payloadSB.AppendLine("};");
			return (payloadSB.ToString(), requestBodyVarName);
		}
		private static void WriteJsonObjectValue(StringBuilder payloadSB, JsonElement value, OpenApiSchema schema, IndentManager indentManager) {
			indentManager.Indent();
			foreach(var property in value.EnumerateObject()) {
				var propertyName = property.Name.ToFirstCharacterUpperCase();
				switch (property.Value.ValueKind) {
					case JsonValueKind.String:
						payloadSB.AppendLine($"{indentManager.GetIndent()}{propertyName} = \"{property.Value.GetString()}\",");
						break;
					case JsonValueKind.Number:
						var numberPropSchema = schema.GetPropertySchema(property.Name);
						payloadSB.AppendLine($"{indentManager.GetIndent()}{propertyName} = {GetNumberLiteral(numberPropSchema, property.Value)},");
						break;
					case JsonValueKind.False:
					case JsonValueKind.True:
						payloadSB.AppendLine($"{indentManager.GetIndent()}{propertyName} = {property.Value.GetBoolean().ToString().ToLowerInvariant()},");
						break;
					case JsonValueKind.Null:
						payloadSB.AppendLine($"{indentManager.GetIndent()}{propertyName} = null,");
						break;
					case JsonValueKind.Object:
						var propSchema = schema.GetPropertySchema(property.Name);
						if(propSchema != null) {
							payloadSB.AppendLine($"{indentManager.GetIndent()}{propertyName} = new {propSchema.GetSchemaTitle().ToFirstCharacterUpperCase()} {{");
							WriteJsonObjectValue(payloadSB, property.Value, propSchema, indentManager);
							payloadSB.AppendLine($"{indentManager.GetIndent()}}},");
						}
						break;
					default://TODO Array
					 	throw new NotImplementedException($"Unsupported JsonValueKind: {property.Value.ValueKind}");
				};
			}
			indentManager.Unindent();
		}
		private static string GetNumberLiteral(OpenApiSchema schema, JsonElement value) {
			if(schema == default) return default;
			return schema.Type switch {
				"number" when schema.Format.Equals("double") => $"{value.GetDouble()}d",
				"number" when schema.Format.Equals("float") => $"{value.GetDecimal()}f",
				"integer" when schema.Format.Equals("int64") => $"{value.GetInt64()}L",
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