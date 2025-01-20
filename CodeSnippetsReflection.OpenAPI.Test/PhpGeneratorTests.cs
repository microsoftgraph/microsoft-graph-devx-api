using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class PhpGeneratorTests : OpenApiSnippetGeneratorTestBase
{
    private readonly PhpGenerator _generator = new();

    [Fact]
    public async Task GeneratesTheCorrectFluentApiPathForIndexedCollectionsAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->messages()->byMessageId('message-id')", result);

    }

    [Fact]
    public async Task GeneratesTheCorrectSnippetForUsersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->me()->get()", result);
    }

    [Fact]
    public async Task GeneratesCorrectLongPathsAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}/messages");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->users()->byUserId('user-id')->messages()->get()->wait();", result);
    }

    [Fact]
    public async Task GeneratesObjectInitializationWithCallToSettersAsync()
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration = new UsersRequestBuilderPostRequestConfiguration();", result);
        Assert.Contains("$queryParameters = UsersRequestBuilderPostRequestConfiguration::createQueryParameters();", result);
    }

    [Fact]
    public async Task IncludesRequestBodyClassNameAsync()
    {
        const string payloadBody =
            "{\r\n  \"passwordCredential\": {\r\n    \"displayName\": \"Password friendly name\"\r\n  }\r\n}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/applications/{{id}}/addPassword")
            {
                Content = new StringContent(payloadBody, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("AddPasswordPostRequestBody", result);
        Assert.Contains(@"use Microsoft\Graph\Beta\GraphServiceClient;", result);
        Assert.Contains(@"use Microsoft\Graph\Beta\Generated\Models\PasswordCredential;", result);
    }

    [Fact]
    public async Task FindsPathItemsWithDifferentCasingAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$result = $graphServiceClient->directory()->deletedItems()->graphGroup()->get()->wait();", result);
    }

    [Fact]
    public async Task DoesntFailOnTerminalSlashAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(
            "$graphServiceClient->me()->messages()->byMessageId('message-id')->get($requestConfiguration)",
            result);
    }

    [Fact]
    public async Task GeneratesFilterParametersAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration->queryParameters = $queryParameters;", result);
        Assert.Contains("$queryParameters = UsersRequestBuilderGetRequestConfiguration::createQueryParameters();", result);
        Assert.Contains("$queryParameters->orderby = [\"displayName\"];", result);
        Assert.Contains("$queryParameters->select = [\"id\",\"displayName\",\"department\"];", result);
        Assert.Contains("$queryParameters->filter = \"Department eq 'Finance'\";", result);
    }
    
    [Fact]
    public async Task GeneratesRequestHeadersAsync() {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
        requestPayload.Headers.Add("ConsistencyLevel", "eventual");
        requestPayload.Headers.Add("Accept", "application/json");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$headers = [", result);
        Assert.Contains("'ConsistencyLevel' => 'eventual',", result);
        Assert.Contains("'Accept' => 'application/json',", result);
    }

    [Fact]
    public async Task GenerateForGroupPostTestSpacingAsync()
    {
        const string body = @"
            {
                ""description"": ""Self help community for library"",
                ""displayName"": ""Library Assist"",
                ""groupTypes"": [
                ""Unified""
                    ],
                ""mailEnabled"": true,
                ""mailNickname"": ""library"",
                ""securityEnabled"": false
            }
        ";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains($"$requestBody = new Group();{Environment.NewLine}" +
                        $"$requestBody->setDescription('Self help community for library');{Environment.NewLine}" +
                        "$requestBody->setDisplayName('Library Assist');", result);
    }
    [Fact]
    public async Task GeneratesComplicatedObjectsWithNestingAsync()
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
            "\"categories\": [\"one\", \"category\", \"away\", null], \"webLink\": null  },\r\n  \"saveToSentItems\": false\r\n}";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail")
                {
                    Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("$message->setCcRecipients($ccRecipientsArray);", result);
            Assert.Contains("$ccRecipientsArray []= $ccRecipientsRecipient1;", result);
            Assert.Contains("$message->setWebLink(null);", result);
    }
    
    [Fact]
    public async Task GeneratesDeleteRequestAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{id}}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphServiceClient->me()->messages()->byMessageId('message-id')->delete()", result);
    }
    
    [Fact]
    public async Task GenerateForRequestBodyCornerCaseAsync()
    {
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages/{{id}}/createReply")
            {
                Content = new StringContent("{\"field\":\"Nothinkg to be done\"}", Encoding.UTF8, "application/json")
            };
       var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
       await snippetModel.InitializeModelAsync(requestPayload);
       var result = _generator.GenerateCodeSnippet(snippetModel);
       Assert.Contains("$requestBody = new CreateReplyPostRequestBody();", result);
    }

    [Fact]
    public async Task GenerateForRefRequestsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
            $"{ServiceRootBetaUrl}/applications/{{application-id}}/tokenIssuancePolicies/$ref");

        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);

        var result = _generator.GenerateCodeSnippet(snippetModel);

        Assert.Contains("->ref()", result);
    }

    [Fact]
    public async Task GenerateForPostBodyWithEnumsAsync()
    {
        var body = "{\"state\": \"active\", \"serviceIdentifier\":\"id\", \"catalogId\":\"id\"}";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/users/{{user%2Did}}/usageRights")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setState(new UsageRightState('active'));", result);
    }

    [Fact]
    public async Task GenerateForComplexCornerCaseAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/identityGovernance/appConsent/appConsentRequests/{{id}}/userConsentRequests/filterByCurrentUser(on='reviewer')");

        var betaTreeNode = await GetBetaSnippetMetadataAsync();
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, betaTreeNode);
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("appConsent", result);
    }

    [Fact]
    public async Task GenerateComplexBodyNameAsync()
    {
        var url = "/devices/{id}/registeredUsers/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"field\":\"Nothing to be done\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setAdditionalData($additionalData);", result);
    }

    [Fact]
    public async Task GenerateFluentApiPathCornerCaseAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/activities/recent");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$result = $graphServiceClient->me()->activities()->recent()->get()->wait();", result);
    }

    [Fact /*(Skip = "Should fail by default.")*/]
    public async Task GenerateWithODataTypeAndODataIdAsync()
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setCallOptions($callOptions);", result);
    }
    
    [Fact]
    public async Task GenerateFindMeetingTimeAsync()
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("new AttendeeBase();", result);
        Assert.Contains("new LocationConstraintItem();", result);
    }

    [Fact]
    public async Task GenerateSnippetsWithArrayNestingAsync()
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$location = new Location();", result);
        Assert.Contains("$requestBody->setAttendees($attendeesArray);", result);
    }

    [Fact]
    public async Task GenerateWithValidRequestBodyAsync()
    {
        var url = "/groups/{id}/acceptedSenders/$ref";
        
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/users/alexd@contoso.com\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("= new ReferenceCreate();", result);
    }

    [Fact]
    public async Task GenerateWithOdataIdAsync()
    {
        var url = "/devices/{id}/registeredUsers/$ref";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent("{\"@odata.id\":\"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"}", Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("= new ReferenceCreate();", result);
    }

    [Fact]
    public async Task GenerateWithFiltersAsync()
    {
        const string url =
            "/identityGovernance/lifecycleWorkflows/workflows/{workflowId}/userProcessingResults/summary(startDateTime={TimeStamp},endDateTime={TimeStamp})";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/{url}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("new \\DateTime('{endDateTime}'),new \\DateTime('{startDateTime}')", result);
    }

    [Fact]
    public async Task GenerateForArraysOfEnumsAsync()
    {
        const string url = "/users/{userId}/settings/shiftPreferences";

        const string body = @"
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
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->setDaysOfWeek([new DayOfWeek('monday'),new DayOfWeek('wednesday'),new DayOfWeek('friday'),	]);", result);
    }

    [Fact]
    public async Task GenerateForOnPathParametersAsync()
    {
        const string url = "/identityGovernance/appConsent/appConsentRequests/filterByCurrentUser(on='parameterValue')";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{url}");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("->identityGovernance()->appConsent()->appConsentRequests()->filterByCurrentUserWithOn('reviewer', )->get()", result);
    }
    [Fact]
    public async Task GenerateForEscapedTypesAsync()
    {
        const string url = "/sites/{site-id}/lists";
        const string body = @"{
                  ""displayName"": ""Books"",
                  ""columns"": [
                    {
                        ""name"": ""Author"",
                        ""text"": { }
                    },
                    {
                        ""name"": ""PageCount"",
                        ""number"": { }
                    }
                    ],
                    ""list"": {
                        ""template"": ""genericList""
                    }
                 }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody = new EscapedList();", result);
    }
    
    [Fact]
    public async Task GenerateForComplexMapValuesAsync()
    {
        const string url = "/communications/calls/{id}/transfer";
        const string body = @"
                {
                  ""transferTarget"": {
                        ""endpointType"": ""default"",
                        ""identity"": {
                            ""phone"": {
                                ""@odata.type"": ""#microsoft.graph.identity"",
                                ""id"": ""+12345678901""
                            }
                        },
                        ""languageId"": ""languageId-value"",
                        ""region"": ""region-value""
                    },
                    ""clientContext"": ""9e90d1c1-f61e-43e7-9f75-d420159aae08""
                }
       ";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(@"'phone' => [", result);
        Assert.Contains("'@odata.type' => '#microsoft.graph.identity',", result);
        Assert.Contains("'id' => '+12345678901',", result);
    }

    [Fact]
    public async Task GenerateForMoreComplexMappingAsync()
    {
        const string url = "/planner/tasks/{task-id}/details";
        const string body = @"
               {
              ""previewType"": ""noPreview"",
                    ""references"": {
                        ""http%3A//developer%2Emicrosoft%2Ecom"":{
                            ""@odata.type"": ""microsoft.graph.plannerExternalReference"",
                            ""alias"": ""Documentation"",
                            ""previewPriority"": "" !"",
                            ""type"": ""Other""
                        },
                        ""https%3A//developer%2Emicrosoft%2Ecom/en-us/graph/graph-explorer"":{
                            ""@odata.type"": ""microsoft.graph.plannerExternalReference"",
                            ""previewPriority"": ""  !!"",
                        },
                        ""http%3A//www%2Ebing%2Ecom"": null
                    },
                    ""checklist"": {
                        ""95e27074-6c4a-447a-aa24-9d718a0b86fa"":{
                            ""@odata.type"": ""microsoft.graph.plannerChecklistItem"",
                            ""title"": ""Update task details"",
                            ""isChecked"": true
                        },
                        ""d280ed1a-9f6b-4f9c-a962-fb4d00dc50ff"":{
                            ""@odata.type"": ""microsoft.graph.plannerChecklistItem"",
                            ""isChecked"": true,
                        },
                        ""a93c93c5-10a6-4167-9551-8bafa09967a7"": null
                    }
                }
       ";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("'http%3A//developer%2Emicrosoft%2Ecom' => [", result);
        Assert.Contains("'https%3A//developer%2Emicrosoft%2Ecom/en-us/graph/graph-explorer' => [", result);
    }

    [Fact]
    public async Task GenerateForNestedObjectInsideMapAsync()
    {
        const string url = "/security/triggers/retentionEvents";
        const string body = @"
            {
              ""@odata.type"": ""#microsoft.graph.security.retentionEvent"",
                    ""displayName"": ""String"",
                    ""description"": ""String"",
                    ""eventQuery"": [
                    {
                        ""@odata.type"": ""microsoft.graph.security.eventQuery""
                    }
                    ],
                    ""eventTriggerDateTime"": ""String (timestamp)"",
                    ""retentionEventType@odata.bind"": ""https://graph.microsoft.com/v1.0/security/triggerTypes/retentionEventType/9eecef97-fb3c-4c68-825b-4dd74530863a""
            }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("'eventQuery' => [", result);
        Assert.Contains("'@odata.type' => 'microsoft.graph.security.eventQuery',", result);
    }

    [Fact]
    public async Task GenerateWithMoreMapsWithArrayOfObjectsAsync()
    {
        const string url = "/identityGovernance/lifecycleWorkflows/workflows/{workflowId}/createNewVersion";
        const string body = @"
            {
                ""category"": ""joiner"",
                    ""description"": ""Configure new hire tasks for onboarding employees on their first day"",
                    ""displayName"": ""custom email marketing API test"",
                    ""isEnabled"": true,
                    ""isSchedulingEnabled"": false,
                    ""executionConditions"": {
                        ""@odata.type"": ""#microsoft.graph.identityGovernance.triggerAndScopeBasedConditions"",
                        ""scope"": {
                            ""@odata.type"": ""#microsoft.graph.identityGovernance.ruleBasedSubjectSet"",
                            ""rule"": ""(department eq 'Marketing')""
                        },
                        ""trigger"": {
                            ""@odata.type"": ""#microsoft.graph.identityGovernance.timeBasedAttributeTrigger"",
                            ""timeBasedAttribute"": ""employeeHireDate"",
                            ""offsetInDays"": 0
                        }
                    },
                    ""tasks"": [
                    {
                        ""continueOnError"": false,
                        ""description"": ""Enable user account in the directory"",
                        ""displayName"": ""Enable User Account"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""6fc52c9d-398b-4305-9763-15f42c1676fc"",
                        ""arguments"": []
                    },
                    {
                        ""continueOnError"": false,
                        ""description"": ""Send welcome email to new hire"",
                        ""displayName"": ""Send Welcome Email"",
                        ""isEnabled"": true,
                        ""taskDefinitionId"": ""70b29d51-b59a-4773-9280-8841dfd3f2ea"",
                        ""arguments"": [
                        {
                            ""name"": ""cc"",
                            ""value"": ""1baa57fa-3c4e-4526-ba5a-db47a9df95f0""
                        },
                        {
                            ""name"": ""customSubject"",
                            ""value"": ""Welcome to the organization {{userDisplayName}}!""
                        },
                        {
                            ""name"": ""customBody"",
                            ""value"": ""Welcome to our organization {{userGivenName}}!""
                        },
                        {
                            ""name"": ""locale"",
                            ""value"": ""en-us""
                        }
                        ]
                    }
                    ]
            }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("'value' => 'Welcome to the organization {{userDisplayName}}!',", result);
        Assert.Contains("'executionConditions' => [", result);
        Assert.Contains("'scope' => [", result);
        Assert.Contains("'trigger' => [", result);
        Assert.Contains(@"use Microsoft\Graph\Generated\Models\IdentityGovernance\RuleBasedSubjectSet;", result);
    }

    [Fact]
    public async Task GenerateForGuidAsync()
    {
        const string url = "/communications/calls/logTeleconferenceDeviceQuality";
        const string body = @"
        {
          ""quality"": {
            ""@odata.type"": ""#microsoft.graph.teleconferenceDeviceQuality"",
            ""callChainId"": ""0622673d-9f69-49b3-9d4f-5ec64f42ecce"",
            ""participantId"": ""ea078406-b5d4-4d3c-b85e-90103dcec7f6"",
            ""mediaLegId"": ""bd9ee398-4b9d-42c7-8b8d-4e8efad9435f"",
            ""deviceName"": ""TestAgent"",
            ""deviceDescription"": ""TestDescription"",
            ""mediaQualityList"": [
              {
                ""@odata.type"": ""#microsoft.graph.teleconferenceDeviceAudioQuality"",
                ""channelIndex"": 1,
                ""mediaDuration"": ""PT20M"",
                ""networkLinkSpeedInBytes"": 13000,
                ""localIPAddress"": ""127.0.0.1"",
                ""localPort"": 6300,
                ""remoteIPAddress"": ""102.1.1.101"",
                ""remotePort"": 6301,
                ""inboundPackets"": 5500,
                ""outboundPackets"": 5400,
                ""averageInboundPacketLossRateInPercentage"": 0.01,
                ""averageOutboundPacketLossRateInPercentage"": 0.02,
                ""maximumInboundPacketLossRateInPercentage"": 0.05,
                ""maximumOutboundPacketLossRateInPercentage"": 0.06,
                ""averageInboundRoundTripDelay"": ""PT0.03S"",
                ""averageOutboundRoundTripDelay"": ""PT0.04S"",
                ""maximumInboundRoundTripDelay"": ""PT0.13S"",
                ""maximumOutboundRoundTripDelay"": ""PT0.14S"",
                ""averageInboundJitter"": ""PT0.01S"",
                ""averageOutboundJitter"": ""PT0.015S"",
                ""maximumInboundJitter"": ""PT0.023S"",
                ""maximumOutboundJitter"": ""PT0.024S""
              },
              {
                ""@odata.type"": ""#microsoft.graph.teleconferenceDeviceVideoQuality"",
                ""channelIndex"": 1,
                ""mediaDuration"": ""PT20M"",
                ""networkLinkSpeedInBytes"": 13000,
                ""localIPAddress"": ""127.0.0.1"",
                ""localPort"": 6300,
                ""remoteIPAddress"": ""102.1.1.101"",
                ""remotePort"": 6301,
                ""inboundPackets"": 5500,
                ""outboundPackets"": 5400,
                ""averageInboundPacketLossRateInPercentage"": 0.01,
                ""averageOutboundPacketLossRateInPercentage"": 0.02,
                ""maximumInboundPacketLossRateInPercentage"": 0.05,
                ""maximumOutboundPacketLossRateInPercentage"": 0.06,
                ""averageInboundRoundTripDelay"": ""PT0.03S"",
                ""averageOutboundRoundTripDelay"": ""PT0.04S"",
                ""maximumInboundRoundTripDelay"": ""PT0.13S"",
                ""maximumOutboundRoundTripDelay"": ""PT0.14S"",
                ""averageInboundJitter"": ""PT0.01S"",
                ""averageOutboundJitter"": ""PT0.015S"",
                ""maximumInboundJitter"": ""PT0.023S"",
                ""maximumOutboundJitter"": ""PT0.024S""
              },
              {
                ""@odata.type"": ""#microsoft.graph.teleconferenceDeviceScreenSharingQuality"",
                ""channelIndex"": 1,
                ""mediaDuration"": ""PT20M"",
                ""networkLinkSpeedInBytes"": 13000,
                ""localIPAddress"": ""127.0.0.1"",
                ""localPort"": 6300,
                ""remoteIPAddress"": ""102.1.1.101"",
                ""remotePort"": 6301,
                ""inboundPackets"": 5500,
                ""outboundPackets"": 5400,
                ""averageInboundPacketLossRateInPercentage"": 0.01,
                ""averageOutboundPacketLossRateInPercentage"": 0.02,
                ""maximumInboundPacketLossRateInPercentage"": 0.05,
                ""maximumOutboundPacketLossRateInPercentage"": 0.06,
                ""averageInboundRoundTripDelay"": ""PT0.03S"",
                ""averageOutboundRoundTripDelay"": ""PT0.04S"",
                ""maximumInboundRoundTripDelay"": ""PT0.13S"",
                ""maximumOutboundRoundTripDelay"": ""PT0.14S"",
                ""averageInboundJitter"": ""PT0.01S"",
                ""averageOutboundJitter"": ""PT0.015S"",
                ""maximumInboundJitter"": ""PT0.023S"",
                ""maximumOutboundJitter"": ""PT0.024S""
              }
            ]
          }
        }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("new \\DateInterval(", result);
        Assert.Contains("->setCallChainId('0622673d-9f69-49b3-9d4f-5ec64f42ecce')", result);
    }

    [Fact]
    public async Task GenerateWithCustomDateAndTimeTypesAsync()
    {
        const string url = "/deviceManagement/deviceConfigurations/{deviceConfigurationId}";
        const string body = @"
          {
            ""@odata.type"": ""#microsoft.graph.windowsUpdateForBusinessConfiguration"",
            ""description"": ""Description value"",
            ""displayName"": ""Display Name value"",
            ""version"": 7,
            ""deliveryOptimizationMode"": ""httpOnly"",
            ""prereleaseFeatures"": ""settingsOnly"",
            ""automaticUpdateMode"": ""notifyDownload"",
            ""microsoftUpdateServiceAllowed"": true,
            ""driversExcluded"": true,
            ""installationSchedule"": {
                ""@odata.type"": ""microsoft.graph.windowsUpdateScheduledInstall"",
                ""scheduledInstallDay"": ""everyday"",
                ""scheduledInstallTime"": ""11:59:31.3170000""
            },
            ""qualityUpdatesDeferralPeriodInDays"": 2,
            ""featureUpdatesDeferralPeriodInDays"": 2,
            ""qualityUpdatesPaused"": true,
            ""featureUpdatesPaused"": true,
            ""qualityUpdatesPauseExpiryDateTime"": ""2017-01-01T00:00:22.9594683-08:00"",
            ""featureUpdatesPauseExpiryDateTime"": ""2016-12-31T23:58:08.068669-08:00"",
            ""businessReadyUpdatesOnly"": ""all"",
            ""skipChecksBeforeRestart"": true,
            ""updateWeeks"": ""firstWeek"",
            ""qualityUpdatesPauseStartDate"": ""2016-12-31"",
            ""featureUpdatesPauseStartDate"": ""2016-12-31"",
            ""featureUpdatesRollbackWindowInDays"": 2,
            ""qualityUpdatesWillBeRolledBack"": true,
            ""featureUpdatesWillBeRolledBack"": true,
            ""qualityUpdatesRollbackStartDateTime"": ""2016-12-31T23:57:01.05526-08:00"",
            ""featureUpdatesRollbackStartDateTime"": ""2017-01-01T00:03:21.6080517-08:00"",
            ""engagedRestartDeadlineInDays"": 12,
            ""engagedRestartSnoozeScheduleInDays"": 2,
            ""engagedRestartTransitionScheduleInDays"": 6,
            ""deadlineForFeatureUpdatesInDays"": 15,
            ""deadlineForQualityUpdatesInDays"": 15,
            ""deadlineGracePeriodInDays"": 9,
            ""postponeRebootUntilAfterDeadline"": true,
            ""autoRestartNotificationDismissal"": ""automatic"",
            ""scheduleRestartWarningInHours"": 13,
            ""scheduleImminentRestartWarningInMinutes"": 7,
            ""userPauseAccess"": ""enabled"",
            ""userWindowsUpdateScanAccess"": ""enabled"",
            ""updateNotificationLevel"": ""defaultNotifications"",
            ""allowWindows11Upgrade"": true
        }";
        using var requestPayload =
            new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}{url}")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains(@"use Microsoft\Kiota\Abstractions\Types\Date;", result);
        Assert.Contains(@"use Microsoft\Kiota\Abstractions\Types\Time;", result);
        Assert.Contains(@"use Microsoft\Graph\Generated\Models\AutomaticUpdateMode;", result);
        Assert.Contains("->setQualityUpdatesPauseStartDate(new Date('2016-12-31'))", result);
        Assert.Contains("->setScheduledInstallTime(new Time('11:59:31.3170000'))", result);
    }
	
    [Fact]
    public async Task GenerateWithComplexArrayAsync()
    {
        const string body = @"
          {
          ""description"": ""Group with designated owner and members"",
                ""displayName"": ""Operations group"",
                ""groupTypes"": [
                ],
                ""mailEnabled"": false,
                ""mailNickname"": ""operations2019"",
                ""securityEnabled"": true,
                ""owners@odata.bind"": [
                     ""https://graph.microsoft.com/v1.0/users/26be1845-4119-4801-a799-aea79d09f1a2""
                ],
                ""members@odata.bind"": [
                    ""https://graph.microsoft.com/v1.0/users/ff7cb387-6688-423c-8188-3da9532a73cc"",
                    ""https://graph.microsoft.com/v1.0/users/69456242-0067-49d3-ba96-9de6f2728e14""
                ]
            }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains($"'owners@odata.bind' => [{Environment.NewLine}"+
        "'https://graph.microsoft.com/v1.0/users/26be1845-4119-4801-a799-aea79d09f1a2', ],", result);
    }

    [Fact]
    public async Task GeneratesStreamInterfaceInModelSetterAsync()
    {
        var body = @"
        {
            ""dataRecoveryCertificate"": {
                ""@odata.type"": ""microsoft.graph.windowsInformationProtectionDataRecoveryCertificate"",
                ""subjectName"": ""Subject Name value"",
                ""description"": ""Description value"",
                ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-08:00"",
                ""certificate"": ""Y2VydGlmaWNhdGU=""
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/deviceAppManagement/mdmWindowsInformationProtectionPolicies")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$dataRecoveryCertificate->setCertificate(\\GuzzleHttp\\Psr7\\Utils::streamFor(base64_decode('Y2VydGlmaWNhdGU=')));", result);
    }

    [Fact]
    public async Task ReplacesReservedWordsInFluentApiPathAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/print/printers/{{printerId}}/shares");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphServiceClient->escapedPrint()->printers()->byPrinterId('printer-id')->shares()->get()->wait();", result);
    }

    [Fact]
    public async Task ReplacesValueIdentifierInPathSegementAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/photo/$value");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphServiceClient->me()->photo()->content()->get()->wait();", result);
    }

    [Fact]
    public async Task GeneratesCorrectRequestconfigurationObjectForIndexedCollectionsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{user-id}}?$select=ext55gb1l09_msLearnCourses");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration = new UserItemRequestBuilderGetRequestConfiguration();", result);
    }

    [Fact]
    public async Task GeneratesRequestConfigurationClassNameWithMeEndpointsAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$expand=manager($levels=max;$select=id,displayName)&$filter=distributionMethod eq 'organization'");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration = new UserItemRequestBuilderGetRequestConfiguration();", result);
        Assert.Contains("$queryParameters->expand = [\"manager(\\$levels=max;\\$select=id,displayName)\"]", result);
        Assert.Contains("$queryParameters->filter = \"distributionMethod eq 'organization'\"", result);
    }

    [Fact]
    public async Task EscapesRequestBodySetterNamesAsync()
    {
        var body = @"
        {
            ""displayName"": ""Books"",
            ""list"": {
                ""template"": ""genericList""
            }
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/sites/{{site-id}}/lists")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestBody->setEscapedList($list);", result);
    }

    [Fact]
    public async Task CleansUnderscoresFromSettersAsync()
    {
        var body = @"
        {
            ""@odata.type"": ""#microsoft.graph.androidLobApp"",
            ""minimumSupportedOperatingSystem"": {
                ""@odata.type"": ""microsoft.graph.androidMinimumOperatingSystem"",
                ""v8_0"": true,
                ""v8_1"": true,
                ""v10_0"": true
            },
        }";
        using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/deviceAppManagement/mobileApps")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$minimumSupportedOperatingSystem->setV80(true);", result);
        Assert.Contains("$minimumSupportedOperatingSystem->setV81(true);", result);
        Assert.Contains("$minimumSupportedOperatingSystem->setV100(true);", result);
    }

    [Fact]
    public async Task GeneratesFromHyphenSeparatedRequestBuilderPathAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/policies/crossTenantAccessPolicy/partners/{{crossTenantAccessPolicyConfigurationPartner-tenantId}}/identitySynchronization");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$graphServiceClient->policies()->crossTenantAccessPolicy()->partners()->byCrossTenantAccessPolicyConfigurationPartnerTenantId('crossTenantAccessPolicyConfigurationPartner-tenantId')->identitySynchronization()->delete()->wait();", result);
    }

    [Fact]
    public async Task GeneratesCorrectRequestConfigNameWithOdataCastAsync()
    {
        using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups/{{id}}/members/microsoft.graph.user?$count=true&$orderby=displayName");
        var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
        await snippetModel.InitializeModelAsync(requestPayload);
        var result = _generator.GenerateCodeSnippet(snippetModel);
        Assert.Contains("$requestConfiguration = new GraphUserRequestBuilderGetRequestConfiguration();", result);
    }
}
