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
		public void GeneratesTheGetMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("GetAsync", result);
			Assert.Contains("await", result);
		}
		[Fact]
		public void GeneratesThePostMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("PostAsync", result);
		}
		[Fact]
		public void GeneratesThePatchMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("PatchAsync", result);
		}
		[Fact]
		public void GeneratesThePutMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("PutAsync", result);
		}
		[Fact]
		public void GeneratesTheDeleteMethodCall() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("DeleteAsync", result);
			Assert.DoesNotContain("var result =", result);
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
		[Fact]
		public void WritesALongAndFindsAnAction() {
			const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

			using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
			{
				Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
			};
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("10L", result);
			Assert.DoesNotContain("microsoft.graph", result);
		}
		[Fact]
		public void WritesADouble() {
			const string userJsonObject = "{\r\n  \"minimumAttendeePercentage\": 10\r\n\r\n}";

			using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
			{
				Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
			};
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("10d", result);
		}
		[Fact]
		public void GeneratesABinaryPayload() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
				Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
			};
			requestPayload.Content.Headers.ContentType = new ("application/octect-stream");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("new MemoryStream", result);
		}
		[Fact]
		public void GeneratesABase64UrlPayload() {
			const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
			using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents") {
				Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
			};
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("Encoding.ASCII.GetBytes", result);
		}
		[Fact]
		public void GeneratesAnArrayPayloadInAdditionalData() {
			const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
			using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}") {
				Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
			};
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("new List", result);
			Assert.Contains("AdditionalData", result);
			Assert.Contains("members", result); // property name hasn't been changed
		}
		[Fact]
		public void GeneratesSelectQueryParameters() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("displayName", result);
			Assert.Contains("new GetQueryParameters", result);
		}
		[Fact]
		public void GeneratesCountBooleanQueryParameters() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("displayName", result);
			Assert.DoesNotContain("\"true\"", result);
			Assert.Contains("true", result);
		}
		[Fact]
		public void GeneratesSkipQueryParameters() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.DoesNotContain("\"10\"", result);
			Assert.Contains("10", result);
		}
		[Fact]
		public void GeneratesSelectExpandQueryParameters() {
			using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
			var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			var result = _generator.GenerateCodeSnippet(snippetModel);
			Assert.Contains("Expand", result);
			Assert.Contains("members($select=id,displayName)", result);
			Assert.DoesNotContain("Select", result);
		}
		//TODO test for request headers
		//TODO test for DateTimeOffset
	}
}