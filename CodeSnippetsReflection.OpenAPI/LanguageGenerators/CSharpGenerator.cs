using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CodeSnippetsReflection.StringExtensions;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
	public class CSharpGenerator : ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>
	{
		private const string clientVarName = "graphClient";
		private const string clientVarType = "GraphClient";
		private const string httpCoreVarName = "httpCore";
		public string GenerateCodeSnippet(SnippetModel snippetModel)
		{
			var snippetBuilder = new StringBuilder(
									"//THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY" + Environment.NewLine +
									$"var {clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
			var (requestPayload, payloadVarName) = GetRequestPayloadAndVariableName(snippetModel);
			snippetBuilder.Append(requestPayload);
			snippetBuilder.AppendLine($"var result = await {clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}.{GetMethodName(snippetModel.Method)}({payloadVarName});");
			return snippetBuilder.ToString();
		}
		private const string requestBodyVarName = "requestBody";
		private static (string, string) GetRequestPayloadAndVariableName(SnippetModel snippetModel) {
			if(string.IsNullOrWhiteSpace(snippetModel?.RequestBody))
				return (default, default);
			
			using var ms = new MemoryStream(UTF8Encoding.UTF8.GetBytes(snippetModel.RequestBody));
			var parsedBody = JsonDocument.Parse(ms);
			var schema = snippetModel.RequestSchema;
			var className = schema.GetSchemaTitle().ToFirstCharacterUpperCase();
			var payloadSB = new StringBuilder($"var {requestBodyVarName} = new {className} {{");

			payloadSB.AppendLine("};");

			return (payloadSB.ToString(), requestBodyVarName);
		}
		private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes) {
			if(!(nodes?.Any() ?? false)) return string.Empty;
			return nodes.Select(x => {
										if(x.Segment.IsCollectionIndex())
											return x.Segment.Replace("{", "[\"").Replace("}", "\"]");
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