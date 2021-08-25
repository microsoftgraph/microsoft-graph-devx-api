using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test {
	public class SnippetModelTests {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
		private static Lazy<OpenApiUrlTreeNode> _v1TreeNode;
		public SnippetModelTests()
		{
			if(_v1TreeNode == null) {
				_v1TreeNode = new Lazy<OpenApiUrlTreeNode>(() => GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml").GetAwaiter().GetResult());
			}
		}
		private static async Task<OpenApiUrlTreeNode> GetTreeNode(string url) {
			using var httpClient = new HttpClient();
			using var response =  await httpClient.GetAsync(url);
			using var stream = await response.Content.ReadAsStreamAsync();
			var reader = new OpenApiStreamReader();
			var doc = reader.Read(stream, out var diags);
			await stream.DisposeAsync();
			return OpenApiUrlTreeNode.Create(doc, "default");
		}
		[Fact]
		public void DefensiveProgramming() {
			var nodeMock = OpenApiUrlTreeNode.Create();
			var requestMock = new Mock<HttpRequestMessage>().Object;
			Assert.Throws<ArgumentNullException>(() => new SnippetModel(null, "something", nodeMock));
			Assert.Throws<ArgumentNullException>(() => new SnippetModel(requestMock, null, nodeMock));
			Assert.Throws<ArgumentNullException>(() => new SnippetModel(requestMock, "something", null));
		}
		[Fact]
		public void FindsThePathItem() {
			const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, _v1TreeNode.Value);
			Assert.NotNull(snippetModel.CurrentNode);
			Assert.NotNull(snippetModel.RootNode);
			Assert.NotEqual(snippetModel.CurrentNode, snippetModel.RootNode);
			Assert.Equal("users", snippetModel.CurrentNode.Segment);
		}
	}
}