using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.ModelGraph;
using System.Collections.Generic;

using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class ImportsGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    [Fact]
    public async Task TestGenerateImportTemplates()
    {
        // Arrange

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




        var importsGenerator = new ImportsGenerator();
        


        // Act
        var result = importsGenerator.GenerateImportTemplates(snippetModel);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<Dictionary<string, string>>>(result);
        Assert.Equal(2, result.Count);
        }

}
