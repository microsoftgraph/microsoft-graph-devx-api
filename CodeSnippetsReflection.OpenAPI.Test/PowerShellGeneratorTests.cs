using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class PowerShellGeneratorTests
    {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private static OpenApiUrlTreeNode _v1TreeNode;
        private readonly PowerShellGenerator _generator = new();
        private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
        {
            if (_v1TreeNode == null)
            {
                _v1TreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            }
            return _v1TreeNode;
        }

        [Fact]
        public async Task GeneratesSnippetForTheGetMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePostMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("New-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePatchMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Update-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePutMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForTheDeleteMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Remove-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSelectQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Property DisplayName,Id", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithFilterQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=displayName eq 'Megan Bowen'");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Filter \"displayName eq 'Megan Bowen'\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithExpandQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-ExpandProperty \"members\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithExpandQueryOptionWithSelect()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-ExpandProperty \"members(`$select=displayName)\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithOrderByQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$orderby=displayName");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Sort \"displayName\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithOrderByQueryOptionWithSortDirection()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$orderby=displayName desc");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Sort \"displayName desc\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithTopQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$top=3");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Top 3", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSkipQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Skip 10", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithCountQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-CountVariable CountVar", result);
            Assert.Contains("-ConsistencyLevel Eventual", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithPathParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/48d31887-5fad-4d73-a9f5-3c356e68a038/appRoleAssignments/hxjTSK1fc02p9Tw1bmigOD83KvI8C1hIl2enozuqhEM");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-UserId $userId -AppRoleAssignmentId $appRoleAssignmentId", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithQueryParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?customParameter=value");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Customparameter \"value\"", result);
        }

        [Theory]
        [InlineData("/me")]
        [InlineData("/me/messages")]
        public async Task GeneratesSnippetsForMeSegment(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-UserId $userId", result);
        }

        [Theory]
        [InlineData("/me")]
        [InlineData("/users/48d31887-5fad-4d73-a9f5-3c356e68a038")]
        public async Task GeneratesSnippetForImportModule(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var firstLine = result.Substring(0, result.IndexOf(Environment.NewLine));
            Assert.StartsWith("Import-Module Microsoft.Graph.Users", firstLine);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOption()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:Megan\"");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Search \"displayName:Megan\"", result);
            Assert.Contains("-ConsistencyLevel Eventual", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithNoBody()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("-BodyParameter", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithBody()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(
                    "{\"displayName\":\"Melissa Darrow\",\"city\":\"Seattle\"}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"DisplayName = \"Melissa Darrow\"{Environment.NewLine}\t" +
                $"City = \"Seattle\"{Environment.NewLine}}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithNestedBody()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(
                    "{\"displayName\":\"Melissa Darrow\"," +
                    "\"city\":\"Seattle\"," +
                    "\"PasswordProfile\":{" +
                    "\"Password\":\"2d79ba3a-b03a-9ed5-86dc-79544e262664\"," +
                    "\"ForceChangePasswordNextSignIn\": false}}", 
                    Encoding.UTF8, 
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"DisplayName = \"Melissa Darrow\"{Environment.NewLine}\t" +
                $"City = \"Seattle\"{Environment.NewLine}\t" +
                $"PasswordProfile = @{{{Environment.NewLine}\t\t" +
                $"Password = \"2d79ba3a-b03a-9ed5-86dc-79544e262664\"{Environment.NewLine}\t\t" +
                $"ForceChangePasswordNextSignIn = $false{Environment.NewLine}\t" +
                $"}}{Environment.NewLine}" +
                $"}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithArrayInBody()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups")
            {
                Content = new StringContent(
                    "{\"displayName\": \"Library Assist\",\"groupTypes\": [\"Unified\",\"DynamicMembership\"]}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"DisplayName = \"Library Assist\"{Environment.NewLine}\t" +
                $"GroupTypes = @({Environment.NewLine}\t\t" +
                $"\"Unified\"{Environment.NewLine}\t\t" +
                $"\"DynamicMembership\"{Environment.NewLine}\t" +
                $"){Environment.NewLine}" +
                $"}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }
    }
}
