using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PhpGeneratorTests
{
    private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
    private static OpenApiUrlTreeNode _v1TreeNode;
    private readonly PhpGenerator _generator = new();
    
    private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
    {
        if (_v1TreeNode == null)
        {
            _v1TreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
        }
        return _v1TreeNode;
    }
    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathForIndexedCollections()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->messagesById('message-id')", result);
    }

    [Fact]
    public async Task GeneratesTheCorrectSnippetForUsers()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->get()", result);
    }

    [Fact]
    public async Task GeneratesCorrectLongPaths()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->usersById('user-id')->messages()->get();", result);
    }
    
    [Fact]
    public async Task WritesADouble()
    {
        const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                      "\"displayName\": \"displayName-value\",\r\n  " +
                                      "\"otherField\": \"NotField\",\r\n  " +
                                      "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                      "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                      " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
        {
            Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("10", result);
    }
}
