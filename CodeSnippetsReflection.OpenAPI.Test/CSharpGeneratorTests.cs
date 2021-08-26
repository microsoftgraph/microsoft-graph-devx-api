using System;
using System.Net.Http;
using System.Text;
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
		public void GeneratesTheCorrectFluentAPIPathForIndexedCollections() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains(".Me.Messages[\"message-id\"]", result);
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
		[Fact]
		public void WritesTheRequestPayload() {
			const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
										  "\"displayName\": \"displayName-value\",\r\n  " +
										  "\"mailNickname\": \"mailNickname-value\",\r\n  " +
										  "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
										  " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

			using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
			{
				Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
			};
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("new User", result);
			Assert.Contains("AccountEnabled = true,", result);
			Assert.Contains("PasswordProfile = new PasswordProfile", result);
			Assert.Contains("DisplayName = \"displayName-value\"", result);
		}

		//TODO test for number types
		//TODO test for arrays
		//TODO test for functions (delta & co)
		//TODO test for PUT/PATCH/DELETE
		//TODO test for query string parameters (select, expand)
		//TODO test for binary data
		//TODO test for request headers
		//TODO test for DateTimeOffset
	}
}