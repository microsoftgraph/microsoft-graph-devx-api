using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;


public class PythonImportTests : OpenApiSnippetGeneratorTestBase
{
    private readonly PythonGenerator _generator = new();

    [Fact]
    public async Task GeneratesRequestBuilderImportsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/calendar/events?$filter=startsWith(subject,'All')");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from kiota_abstractions.base_request_configuration import RequestConfiguration", result);
        Assert.Contains("from msgraph.generated.users.item.calendar.events.events_request_builder import EventsRequestBuilder", result);
    }

    [Fact]
    public async Task GenerateModelImportsAsync(){
        var bodyContent = @"{
            ""displayName"":  ""New display name""
            }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/applications/{{id}}")
        {
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from msgraph.generated.models.application import Application", result);

    }
    [Fact]
    public async Task GenerateComplexModelImportsAsync(){
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from msgraph.generated.models.message import Message", result);
        Assert.Contains("from msgraph.generated.models.item_body import ItemBody", result);
        Assert.Contains("from msgraph.generated.models.recipient import Recipient", result);
        Assert.Contains("from msgraph.generated.models.email_address import EmailAddress", result);
        Assert.Contains("from msgraph.generated.models.extension import Extension", result);
        Assert.Contains("from msgraph.generated.models.open_type_extension import OpenTypeExtension", result);
    }

    [Fact]
    public async Task GenerateNestedRequestBuilderImportsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/applications(appId={{application-id}})?$select=id,appId,displayName,requiredResourceAccess");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from msgraph.generated.applications_with_app_id.applications_with_app_id_request_builder import ApplicationsWithAppIdRequestBuilder", result);
    }
    [Fact]
    public async Task GenerateRequestBodyImportsAsync()
    {
        string bodyContent = @"
        {
            ""passwordCredential"": {
                ""displayName"": ""Test-Password friendly name""
            }
        }";

        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{application-id}}/addPassword"){
            Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from msgraph.generated.models.password_credential import PasswordCredential", result);
        Assert.Contains("from msgraph.generated.applications.item.add_password.add_password_post_request_body import AddPasswordPostRequestBody", result);
    }
    [Fact]
    public async Task GeneratesImportsWithoutFilterAttrbutesInPathAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/servicePrincipals/$count");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("from msgraph import GraphServiceClient", result);
        Assert.Contains("from msgraph.generated.service_principals.count.count_request_builder import CountRequestBuilder", result);
    }
}
