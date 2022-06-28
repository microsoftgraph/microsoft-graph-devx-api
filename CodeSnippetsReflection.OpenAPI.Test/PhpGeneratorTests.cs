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
    private const string ServiceRootBetaUrl = "https://graph.microsoft.com/beta";
    private static OpenApiUrlTreeNode _v1TreeNode;
    private static OpenApiUrlTreeNode _betaTreeNode;
    private readonly PhpGenerator _generator = new();

    private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
    {
        if (_v1TreeNode == null)
        {
            _v1TreeNode = await SnippetModelTests.GetTreeNode(
                "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
        }

        return _v1TreeNode;
    }

    private static async Task<OpenApiUrlTreeNode> GetBetaTreeNode()
    {
        if (_betaTreeNode == null)
        {
            _betaTreeNode = await SnippetModelTests.GetTreeNode(
                "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml");
        }

        return _betaTreeNode;
    }

    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathForIndexedCollections()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
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
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->usersById('user-id')->messages()->get();", result);
    }

    [Fact]
    public async Task GeneratesObjectInitializationWithCallToSetters()
    {
        const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                      "\"displayName\": \"displayName-value\",\r\n  " +
                                      "\"otherField\": \"NotField\",\r\n  " +
                                      "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                      "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                      " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n, \"added\": \"somethingWeird\" }\r\n, \"papa\": [3,4,5,6, true, false, \"yoda\"]}";

        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/?$select=displayName,mailNickName")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$queryParameters = new UsersRequestBuilderPostQueryParameters();", result);
        Assert.Contains("$requestConfiguration->queryParameters = $queryParameters;", result);
    }

    [Fact]
    public async Task IncludesRequestBodyClassName()
    {
        const string payloadBody =
            "{\r\n  \"passwordCredential\": {\r\n    \"displayName\": \"Password friendly name\"\r\n  }\r\n}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/applications/{{id}}/addPassword")
            {
                Content = new StringContent(payloadBody, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("PasswordCredentialRequestBody", result);
    }

    [Fact]
    public async Task FindsPathItemsWithDifferentCasing()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphClient->directory()->deletedItems()->group()->get()", result);
    }

    [Fact]
    public async Task DoesntFailOnTerminalSlash()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(
            "$graphClient->me()->messagesById('message-id')->get($requestConfiguration)",
            result);
    }

    [Fact]
    public async Task GeneratesFilterParameters()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$queryParameters->count = true;", result);
        Assert.Contains("$queryParameters->filter", result);
        Assert.Contains("$queryParameters->select", result);
        Assert.Contains("$queryParameters->orderBy", result);
    }
    
    [Fact]
    public async Task GeneratesRequestHeaders() {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        requestPayload.Headers.Add("Accept", "application/json");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$headers = [", result);
        Assert.Contains("\"ConsistencyLevel\" => \"eventual\",", result);
        Assert.Contains("$requestConfiguration = ", result);
    }

    [Fact]
    public async Task GeneratesComplicatedObjectsWithNesting()
    {
        const string userJsonObject =
            "{\r\n  \"message\": {\r\n    " +
            "\"subject\": \"Meet for lunch?\",\r\n    " +
            "\"body\": {\r\n      \"contentType\": \"Text\",\r\n      " +
            "\"content\": \"The new cafeteria is open.\"\r\n    },\r\n    " +
            "\"toRecipients\": [\r\n      {\r\n        " +
            "\"emailAddress\": {\r\n         " +
            " \"address\": \"fannyd@contoso.onmicrosoft.com\"\r\n        }\r\n      },\r\n        {\r\n        " +
            "\"emailAddress\": {\r\n                      \"address\": \"jose@con'stoso.onmicrosoft.com\"\r\n        }\r\n      }\r\n    ],\r\n   " +
            " \"ccRecipients\": [\r\n      {\r\n        \"emailAddress\": {\r\n          " +
            "\"address\": \"danas@contoso.onmicrosoft.com\"\r\n        }\r\n      }\r\n    ]\r\n," +
            "\"categories\": [\"one\", \"category\", \"away\"]  },\r\n  \"saveToSentItems\": \"false\"\r\n}";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail")
                {
                    Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("$message->setCcRecipients($ccRecipientsArray);", result);
            Assert.Contains("$ccRecipientsArray []= $ccRecipientsccRecipients1;", result);
    }
    
    [Fact]
    public async Task GeneratesDeleteRequest()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphClient->me()->messagesById('message-id')->delete()", result);
    }
    
    [Fact]
    public async Task GenerateForRequestBodyCornerCase()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages/{{id}}/createReply")
            {
                Content = new StringContent("{\"field\":\"Nothinkg to be done\"}", Encoding.UTF8, "application/json")
            };
       var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
       var result = _generator.GenerateCodeSnippet(snippetModel);
       Assert.Contains("$requestRequestBody = new CreateReplyPostRequestBody();", result);
    }

    [Fact]
    public async Task GenerateForRefRequests()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/applications/{{application-id}}/tokenIssuancePolicies/$ref");

        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());

        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("->ref()", result);
    }

    [Fact]
    public async Task GenerateForPostBodyWithEnums()
    {
        var body = "{\"state\": \"active\", \"serviceIdentifier\":\"id\", \"catalogId\":\"id\"}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/users/{{user%2Did}}/usageRights")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestRequestBody->setState(new UsageRightState('active'));", result);
    }

    [Fact]
    public async Task GenerateForComplexCornerCase()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/identityGovernance/appConsent/appConsentRequests/{{id}}/userConsentRequests/filterByCurrentUser(on='reviewer')");

        var betaTreeNode = await GetBetaTreeNode();
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, betaTreeNode);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("appConsent", result);
    }
}
