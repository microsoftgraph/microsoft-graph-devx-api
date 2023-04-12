using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PythonGeneratorTests
{
    private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
    private const string ServiceRootBetaUrl = "https://graph.microsoft.com/beta";
    private static OpenApiUrlTreeNode _v1TreeNode;
    private static OpenApiUrlTreeNode _betaTreeNode;
    private readonly PythonGenerator _generator = new();

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
        Assert.Contains(".me.messages_by_id('message-id').get()", result);
    }

    [Fact]
    public async Task GeneratesTheCorrectSnippetForUsers()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.get()", result);
    }

    [Fact]
    public async Task GeneratesCorrectLongPaths()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".users_by_id('user-id').messages.get()", result);
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
        // Assert.Contains(
        // @"query_params = UsersRequestBuilder.UsersRequestBuilderPostQueryParameters(
        //     select = [""displayName"",""mailNickName""],
        //     )", result);
        Assert.Contains("query_parameters = query_params", result);
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
        Assert.Contains("AddPasswordPostRequestBody", result);
    }

    [Fact]
    public async Task FindsPathItemsWithDifferentCasing()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".directory.deletedItems.graphgroup.get()", result);
    }

    [Fact]
    public async Task DoesntFailOnTerminalSlash()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(
            ".me.messages_by_id('message-id').get(request_configuration = request_configuration)",
            result);
    }

    [Fact]
    public async Task GeneratesFilterParameters()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("count = true", result);
        Assert.Contains(@"select = [""id"",""displayName"",""department""]", result);
        Assert.Contains(@"orderby = [""displayName""]", result);
    }
    
    [Fact]
    public async Task GeneratesRequestHeaders() {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        requestPayload.Headers.Add("Accept", "application/json");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("headers = {", result);
        Assert.Contains(@"'ConsistencyLevel' : ""eventual""", result);
        Assert.Contains("request_configuration = request_configuration", result);
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
            "\"address\": null\r\n        }\r\n      }\r\n    ]\r\n," +
            "\"categories\": [\"one\", \"category\", \"away\", null]  },\r\n  \"saveToSentItems\": false\r\n}";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail")
                {
                    Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("ccRecipientsRecipient1 = Recipient()", result);
            Assert.Contains("message.body = messageBody", result);
    }
    
    [Fact]
    public async Task GeneratesDeleteRequest()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.messages_by_id('message-id').delete()", result);
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
       Assert.Contains("request_body = CreateReplyPostRequestBody()", result);
    }

    [Fact]
    public async Task GenerateForRefRequests()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/applications/{{application-id}}/tokenIssuancePolicies/$ref");

        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());

        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains(".ref.get()", result);
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
        Assert.Contains("request_body = UsageRight()", result);
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

    [Fact]
    public async Task GenerateComplexBodyName()
    {
        var url = "/devices/{id}/registeredUsers/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"field\":\"Nothing to be done\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body.additionaldata(additionalData)", result);
    }

    [Fact]
    public async Task GenerateFluentApiPathCornerCase()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/activities/recent");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(".me.activities.recent.get()", result);
    }

    [Fact /*(Skip = "Should fail by default.")*/]
    public async Task GenerateWithODataTypeAndODataId()
    {
        var url = "/communications/calls/{id}/answer";
        var bodyContent = @"
            {
              ""callbackUri"": ""callbackUri-value"",
                    ""mediaConfig"": {
                        ""@odata.type"": ""#microsoft.graph.appHostedMediaConfig"",
                        ""blob"": ""<Media Session Configuration Blob>""
                    },
                    ""acceptedModalities"": [
                    ""audio""
                        ],
                    ""callOptions"": {
                        ""@odata.type"": ""#microsoft.graph.incomingCallOptions"",
                        ""isContentSharingNotificationEnabled"": true
                    },
                    ""participantCapacity"": 200
                }
            ";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}{url}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("request_body.callOptions = callOptions", result);
    }
    
    [Fact]
    public async Task GenerateFindMeetingTime()
    {
        var bodyContent = @"
        {
            ""attendees"": [
                {
                    ""emailAddress"": {
                        ""address"": ""{user-mail}"",
                        ""name"": ""Alex Darrow""
                    },
                    ""type"": ""Required""
                }
                ],
                ""timeConstraint"": {
                    ""timeslots"": [
                    {
                        ""start"": {
                            ""dateTime"": ""2022-07-18T13:24:57.384Z"",
                            ""timeZone"": ""Pacific Standard Time""
                        },
                        ""end"": {
                            ""dateTime"": ""2022-07-25T13:24:57.384Z"",
                            ""timeZone"": ""Pacific Standard Time""
                        }
                    }
                    ]
                },
                ""locationConstraint"": {
                    ""isRequired"": ""false"",
                    ""suggestLocation"": ""true"",
                    ""locations"": [
                    {
                        ""displayName"": ""Conf Room 32/1368"",
                        ""locationEmailAddress"": ""conf32room1368@imgeek.onmicrosoft.com""
                    }
                    ]
                },
                ""meetingDuration"": ""PT1H"",
                ""maxCandidates"": ""100"",
                ""isOrganizerOptional"": ""false"",
                ""minimumAttendeePercentage"": ""200""
        }";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("attendeesAttendeeBase1 = AttendeeBase()", result);
        Assert.Contains("= LocationConstraintItem()", result);
    }

    [Fact]
    public async Task GenerateSnippetsWithArrayNesting()
    {
        var eventData = @"
             {
               ""subject"": ""Let's go for lunch"",
                ""body"": {
                    ""contentType"": ""HTML"",
                    ""content"": ""Does noon work for you?""
                },
                ""start"": {
                    ""dateTime"": ""2017-04-15T12:00:00"",
                    ""timeZone"": ""Pacific Standard Time""
                },
                ""end"": {
                    ""dateTime"": ""2017-04-15T14:00:00"",
                    ""timeZone"": ""Pacific Standard Time""
                },
                ""location"":{
                    ""displayName"":""Harry's Bar""
                },
                ""attendees"": [
                {
                    ""emailAddress"": {
                        ""address"":""samanthab@contoso.onmicrosoft.com"",
                        ""name"": ""Samantha Booth""
                    },
                    ""type"": ""required""
                },
                {
                    ""emailAddress"": {
                                    ""address"":""ss@contoso.com"", ""name"": ""Sorry Sir""}, ""type"":""Optional""
                }
                ],
                ""allowNewTimeProposals"": true,
                ""transactionId"":""7E163156-7762-4BEB-A1C6-729EA81755A7""
           }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/events")
            {
                Content = new StringContent(eventData, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("location = Location()", result);
        Assert.Contains("attendeesAttendee1 = Attendee()", result);
    }

    [Fact (Skip = "This is still not passing. Keeping to use during fixing of bug related.")]
    public async Task GenerateWithValidRequestBody()
    {
        var url = "/groups/{id}/acceptedSenders/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/users/alexd@contoso.com\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("= new DirectoryObject();", result);
    }
}
