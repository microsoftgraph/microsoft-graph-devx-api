using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;
using Xunit.Sdk;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PhpGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    private readonly PhpGenerator _generator = new();

    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathForIndexedCollections()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->messages()->byMessageId('message-id')", result);
    }

    [Fact]
    public async Task GeneratesTheCorrectSnippetForUsers()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->get()", result);
    }

    [Fact]
    public async Task GeneratesCorrectLongPaths()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->users()->byUserId('user-id')->messages()->get();", result);
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration = new UsersRequestBuilderPostRequestConfiguration();", result);
        Assert.Contains("$queryParameters = UsersRequestBuilderPostRequestConfiguration::createQueryParameters();", result);
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("AddPasswordPostRequestBody", result);
    }

    [Fact]
    public async Task FindsPathItemsWithDifferentCasing()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$result = $graphServiceClient->directory()->deletedItems()->graphGroup()->get();", result);
    }

    [Fact]
    public async Task DoesntFailOnTerminalSlash()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(
            "$graphServiceClient->me()->messages()->byMessageId('message-id')->get($requestConfiguration)",
            result);
    }

    [Fact]
    public async Task GeneratesFilterParameters()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration->queryParameters = $queryParameters;", result);
        Assert.Contains("$queryParameters = UsersRequestBuilderGetRequestConfiguration::createQueryParameters();", result);
        Assert.Contains("$queryParameters->orderby = [\"displayName\"];", result);
        Assert.Contains("$queryParameters->select = [\"id\",\"displayName\",\"department\"];", result);
        Assert.Contains("$queryParameters->filter = \"Department eq 'Finance'\";", result);
    }
    
    [Fact]
    public async Task GeneratesRequestHeaders() {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        requestPayload.Headers.Add("Accept", "application/json");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$headers = [", result);
        Assert.Contains("'ConsistencyLevel' => 'eventual',", result);
        Assert.Contains("'Accept' => 'application/json',", result);
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("$message->setCcRecipients($ccRecipientsArray);", result);
            Assert.Contains("$ccRecipientsArray []= $ccRecipientsRecipient1;", result);
    }
    
    [Fact]
    public async Task GeneratesDeleteRequest()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphServiceClient->me()->messages()->byMessageId('message-id')->delete()", result);
    }
    
    [Fact]
    public async Task GenerateForRequestBodyCornerCase()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages/{{id}}/createReply")
            {
                Content = new StringContent("{\"field\":\"Nothinkg to be done\"}", Encoding.UTF8, "application/json")
            };
       var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
       var result = _generator.GenerateCodeSnippet(snippetModel);
       Assert.Contains("$requestBody = new CreateReplyPostRequestBody();", result);
    }

    [Fact]
    public async Task GenerateForRefRequests()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/applications/{{application-id}}/tokenIssuancePolicies/$ref");

        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());

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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setState(new UsageRightState('active'));", result);
    }

    [Fact]
    public async Task GenerateForComplexCornerCase()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/identityGovernance/appConsent/appConsentRequests/{{id}}/userConsentRequests/filterByCurrentUser(on='reviewer')");

        var betaTreeNode = await GetBetaSnippetMetadata();
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setAdditionalData($additionalData);", result);
    }

    [Fact]
    public async Task GenerateFluentApiPathCornerCase()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/activities/recent");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$result = $graphServiceClient->me()->activities()->recent()->get();", result);
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setCallOptions($callOptions);", result);
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("new AttendeeBase();", result);
        Assert.Contains("new LocationConstraintItem();", result);
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$location = new Location();", result);
        Assert.Contains("$requestBody->setAttendees($attendeesArray);", result);
    }

    [Fact]
    public async Task GenerateWithValidRequestBody()
    {
        var url = "/groups/{id}/acceptedSenders/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/users/alexd@contoso.com\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("= new ReferenceCreate();", result);
    }

    [Fact]
    public async Task GenerateWithOdataId()
    {
        var url = "/devices/{id}/registeredUsers/$ref";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("= new ReferenceCreate();", result);
    }

    [Fact]
    public async Task GenerateWithFilters()
    {
        var url =
            "/identityGovernance/lifecycleWorkflows/workflows/{workflowId}/userProcessingResults/summary(startDateTime={TimeStamp},endDateTime={TimeStamp})";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/{url}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("new \\DateTime('{endDateTime}'),new \\DateTime('{startDateTime}')", result);
    }

    [Fact]
    public async Task GenerateForArraysOfEnums()
    {
        var url = "/users/{userId}/settings/shiftPreferences";

        var body = @"
        {
            ""id"": ""SHPR_eeab4fb1-20e5-48ca-ad9b-98119d94bee7"",
            ""@odata.etag"": ""1a371e53-f0a6-4327-a1ee-e3c56e4b38aa"",
            ""availability"": [
                {
                    ""recurrence"": {
                        ""pattern"": {
                            ""type"": ""Weekly"",
                            ""daysOfWeek"": [""Monday"", ""Wednesday"", ""Friday""],
                            ""interval"": 1
                        },
                        ""range"": {
                            ""type"": ""noEnd""
                          }
                    },
                    ""timeZone"": ""Pacific Standard Time"",
                    ""timeSlots"": null
            }
           ]
        }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->setDaysOfWeek([new DayOfWeek('monday'),new DayOfWeek('wednesday'),new DayOfWeek('friday'),]);", result);
    }

    [Fact]
    public async Task GenerateForOnPathParameters()
    {
        var url = "/identityGovernance/appConsent/appConsentRequests/filterByCurrentUser(on='parameterValue')";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{url}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->identityGovernance()->appConsent()->appConsentRequests()->filterByCurrentUserWithOn('reviewer', )->get()", result);
    }
}
