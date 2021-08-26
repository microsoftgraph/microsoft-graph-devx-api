using System;
using System.Net.Http;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
	public class CSharpGeneratorTests {
		private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
		private static Lazy<OpenApiUrlTreeNode> _v1TreeNode;
		private readonly CSharpGenerator _generator = new();
		public CSharpGeneratorTests()
		{
			if(_v1TreeNode == null) {
				_v1TreeNode = new Lazy<OpenApiUrlTreeNode>(() => SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml").GetAwaiter().GetResult());
			}
		}
		[Fact]
		public void GeneratesTheCorrectFluentAPIPath() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains(".Me.Messages", result);
		}
		[Fact]
		public void GeneratesTheSnippetHeader() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("var graphClient = new GraphClient(httpCore)", result);
		}
		[Fact]
		public void GeneratesTheCorrectMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("GetAsync", result);
		}
	}
}