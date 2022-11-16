using System.Net.Http;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.Test;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators;

public class GraphCliGeneratorTests
{
    private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
    private static OpenApiUrlTreeNode _v1TreeNode;
    private readonly GraphCliGenerator _generator = new();
    private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
    {
        _v1TreeNode ??= await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml", true);
        return _v1TreeNode;
    }

    [Fact]
    public async Task GeneratesSnippetsForRootCommand()
    {
        // Given
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal("mgc users list", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForDeepSubCommand()
    {
        // /users/{user-id}/authentication/microsoftAuthenticatorMethods/{microsoftAuthenticatorAuthenticationMethod-id}/device/memberOf/{directoryObject-id}
        // Given
        string url = $"{ServiceRootUrl}/users/test-user/authentication/microsoftAuthenticatorMethods/test-method/device/memberOf/test-directory";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal("mgc users item authentication microsoft-authenticator-methods item device member-of item get --user-id {user-id} --microsoft-authenticator-authentication-method-id {microsoftAuthenticatorAuthenticationMethod-id} --directory-object-id {directoryObject-id}", result);
    }

    [Fact]
    public async Task GeneratesSnippetsForCountCommand()
    {
        // /users/{user-id}/authentication/methods/$count
        // Given
        string url = $"{ServiceRootUrl}/users/test-user/authentication/methods/$count";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, url);
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());

        // When
        var result = _generator.GenerateCodeSnippet(snippetModel);

        // Then
        Assert.Equal("mgc users item authentication methods count get --user-id {user-id}", result);
    }
}
