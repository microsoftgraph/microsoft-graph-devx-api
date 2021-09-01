using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class SnippetModelTests
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private static OpenApiUrlTreeNode _v1TreeNode;
        private async static Task<OpenApiUrlTreeNode> GetV1TreeNode() {
            if(_v1TreeNode == null) {
                _v1TreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            }
            return _v1TreeNode;
        }
        internal static async Task<OpenApiUrlTreeNode> GetTreeNode(string url)
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url);
            using var stream = await response.Content.ReadAsStreamAsync();
            var reader = new OpenApiStreamReader();
            var doc = reader.Read(stream, out var diags);
            await stream.DisposeAsync();
            return OpenApiUrlTreeNode.Create(doc, "default");
        }
        [Fact]
        public void DefensiveProgramming()
        {
            var nodeMock = OpenApiUrlTreeNode.Create();
            var requestMock = new Mock<HttpRequestMessage>().Object;
            Assert.Throws<ArgumentNullException>(() => new SnippetModel(null, "something", nodeMock));
            Assert.Throws<ArgumentNullException>(() => new SnippetModel(requestMock, null, nodeMock));
            Assert.Throws<ArgumentNullException>(() => new SnippetModel(requestMock, "something", null));
        }
        [Fact]
        public async Task FindsThePathItem()
        {
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(userJsonObject)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            Assert.NotNull(snippetModel.EndPathNode);
            Assert.NotNull(snippetModel.RootPathNode);
            Assert.Equal(snippetModel.EndPathNode, snippetModel.RootPathNode);
            Assert.Equal("users", snippetModel.EndPathNode.Segment);
            Assert.InRange(snippetModel.PathNodes.Count, 1, 1);
        }
        [Fact]
        public async Task FindsTheSubPathItem()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            Assert.NotNull(snippetModel.EndPathNode);
            Assert.NotNull(snippetModel.RootPathNode);
            Assert.NotEqual(snippetModel.EndPathNode, snippetModel.RootPathNode);
            Assert.Equal("messages", snippetModel.EndPathNode.Segment);
            Assert.InRange(snippetModel.PathNodes.Count, 2, 2);
        }
        [Fact]
        public async Task GetsTheRequestSchema()
        {
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            Assert.NotNull(snippetModel.RequestSchema);
            Assert.NotEmpty(snippetModel.RequestSchema.AllOf);
        }
        [Fact]
        public async Task GetsTheResponseSchema()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            Assert.NotNull(snippetModel.ResponseSchema);
            Assert.NotEmpty(snippetModel.ResponseSchema.Properties);
        }
    }
}