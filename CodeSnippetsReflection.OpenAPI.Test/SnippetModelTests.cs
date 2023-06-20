using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using Moq;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class SnippetModelTests : OpenApiSnippetGeneratorTestBase
    {
        [Fact]
        public void DefensiveProgramming()
        {
            var nodeMock = new OpenApiSnippetMetadata(OpenApiUrlTreeNode.Create(), new Dictionary<string, OpenApiSchema>());
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            Assert.NotNull(snippetModel.RequestSchema);
            Assert.NotEmpty(snippetModel.RequestSchema.AllOf);
        }
        [Fact]
        public async Task GetsTheResponseSchema()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            Assert.NotNull(snippetModel.ResponseSchema);
            Assert.True(snippetModel.ResponseSchema.Properties.Any() ||
                snippetModel.ResponseSchema.AnyOf.Any() ||
                snippetModel.ResponseSchema.AllOf.Any() ||
                snippetModel.ResponseSchema.OneOf.Any());
        }
        
        [Fact]
        public async Task ThrowsExceptionForUnsupportedHttpOperation()
        {
            // DELETE /users/{user-id}/delta
            // Given
            string url = $"{ServiceRootUrl}/users/delta";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, url);//delta exists but has not DELETE

            // When
            // Then
            var entryPointNotFoundException = await Assert.ThrowsAsync<EntryPointNotFoundException>(async () => new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata()));
            Assert.Equal("HTTP Method 'DELETE' not found for path.",entryPointNotFoundException.Message);
        }
        
        [Fact]
        public async Task ThrowsExceptionForUnsupportedPathOperation()
        {
            // GET /users/{user-id}/deltaTest
            // Given
            string url = $"{ServiceRootUrl}/users/100/deltaTest";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, url);//path does not exists

            // When
            // Then
            var entryPointNotFoundException = await Assert.ThrowsAsync<EntryPointNotFoundException>(async () => new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata()));
            Assert.Equal("Path segment 'deltaTest' not found in path",entryPointNotFoundException.Message);
        }
    }
}
