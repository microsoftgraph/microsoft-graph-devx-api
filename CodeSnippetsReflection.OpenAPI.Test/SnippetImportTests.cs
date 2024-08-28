using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using CodeSnippetsReflection.OpenAPI.ModelGraph;

using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class ImportsGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    [Fact]
    public async Task TestGenerateImportTemplatesForModelImportsAsync()
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = ImportsGenerator.GenerateImportTemplates(snippetModel);
        Assert.NotNull(result);
        Assert.IsType<List<ImportTemplate>>(result);
        Assert.Equal(2, result.Count);
    }
    [Fact]
    public async Task TestGenerateImportTemplatesForRequestBuilderImportsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/calendar/events?$filter=startsWith(subject,'All')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = ImportsGenerator.GenerateImportTemplates(snippetModel);
        Assert.NotNull(result);
        Assert.IsType<List<ImportTemplate>>(result);
        Assert.NotNull(result[0].Path);
        Assert.NotNull(result[0].RequestBuilderName);
    }
    [Fact]
    public async Task TestGenerateImportTemplatesForNestedRequestBuilderImportsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/applications(appId={{application-id}})?$select=id,appId,displayName,requiredResourceAccess");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = ImportsGenerator.GenerateImportTemplates(snippetModel);
        Assert.NotNull(result);
        Assert.IsType<List<ImportTemplate>>(result);
        Assert.NotNull(result[0].Path);
        Assert.NotNull(result[0].RequestBuilderName);
    }
}
