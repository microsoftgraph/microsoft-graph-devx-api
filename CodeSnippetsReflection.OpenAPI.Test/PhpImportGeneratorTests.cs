using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PhpImportTests : OpenApiSnippetGeneratorTestBase
{
    private readonly PhpGenerator _generator = new();

    [Fact]
    public async Task GeneratesRequestBuilderImports()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/calendar/events?$filter=startsWith(subject,'All')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("use Microsoft\\Graph\\Graph;", result);
        Assert.Contains("use Microsoft\\Graph\\Generated\\Users\\Item\\Calendar\\Events", result);
    }

    [Fact]
    public async Task GenerateModelImports(){
        var bodyContent = @"{
            ""displayName"":  ""New display name""
            }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/applications/{{id}}")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("use Microsoft\\Graph\\Graph;", result);
        Assert.Contains("use Microsoft\\Graph\\Generated\\Models;", result);

    }
    [Fact]
    public async Task GenerateComplexModelImports(){
        var bodyContent = @"{
            ""subject"": ""Annual review"",
            ""body"": {
                ""contentType"": ""HTML"",
                ""content"": ""You should be proud!""
            },
            ""toRecipients"": [
                {
                ""emailAddress"": {
                    ""address"": ""rufus@contoso.com""
                }
                }
            ],
            ""extensions"": [
                {
                ""@odata.type"": ""microsoft.graph.openTypeExtension"",
                ""extensionName"": ""Com.Contoso.Referral"",
                ""companyName"": ""Wingtip Toys"",
                ""expirationDate"": ""2015-12-30T11:00:00.000Z"",
                ""dealValue"": 10000
                }
            ]
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages/")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("use Microsoft\\Graph\\Graph;", result);
        Assert.Contains("use Microsoft\\Graph\\Generated\\Models;", result);
        
    }
}
