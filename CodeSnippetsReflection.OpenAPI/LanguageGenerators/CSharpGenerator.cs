using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
			var snippetBuilder = new StringBuilder($"var {clientVarName} = new {clientVarType}({httpCoreVarName});{Environment.NewLine}{Environment.NewLine}");
			snippetBuilder.AppendLine($"var result = await {clientVarName}.{GetFluentApiPath(snippetModel.PathNodes)}.{GetMethodName(snippetModel.Method)};");
			return snippetBuilder.ToString();
		}
		private static string GetFluentApiPath(IEnumerable<OpenApiUrlTreeNode> nodes) {
			if(!(nodes?.Any() ?? false)) return string.Empty;
			return nodes.Select(x => x.Segment.ToFirstCharacterUpperCase())
						.Aggregate((x, y) => $"{x}.{y}");
		}
		private static string GetMethodName(HttpMethod method) {
			// can't use pattern matching with switch as it's not an enum but a bunch of static values
			if(method == HttpMethod.Get) return "GetAsync()";
			else if(method == HttpMethod.Post) return "PostAsync()";
			else if(method == HttpMethod.Put) return "PutAsync()";
			else if(method == HttpMethod.Delete) return "DeleteAsync()";
			else if(method == HttpMethod.Patch) return "PatchAsync()";
			else if(method == HttpMethod.Head) return "HeadAsync()";
			else if(method == HttpMethod.Options) return "OptionsAsync()";
			else if(method == HttpMethod.Trace) return "TraceAsync()";
			else throw new InvalidOperationException($"Unsupported HTTP method: {method}");
		}
	}
}