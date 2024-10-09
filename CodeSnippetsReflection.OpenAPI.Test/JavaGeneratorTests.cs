using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class JavaGeneratorTests : OpenApiSnippetGeneratorTestBase
    {
        private readonly JavaGenerator _generator = new();

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPathAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/policies/crossTenantAccessPolicy/default/resetToSystemDefault");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".policies().crossTenantAccessPolicy().defaultEscaped().resetToSystemDefault()", result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPath_2Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/security/alerts_v2");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("alertsV2", result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPath_3Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/print/printers/{{printerId}}/jobs/{{printJobId}}/documents/{{printDocumentId}}/$value");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".content()", result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPath_4Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityGovernance/lifecycleWorkflows/workflows/{{workflowId}}/versions/{{workflowVersion-versionNumber}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("byWorkflowVersionVersionNumber(2)", result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPathForIndexedCollectionsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/security/cases/ediscoveryCases/{{case-id}}/close");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".security().cases().ediscoveryCases().byEdiscoveryCaseId(\"{ediscoveryCase-id}\").microsoftGraphSecurityClose()", result);
        }

        [Fact]
        public async Task GeneratesTheSnippetHeaderAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("GraphServiceClient graphClient = new GraphServiceClient(requestAdapter)", result);
        }

        [Fact]
        public async Task GeneratesTheGetMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("get()", result);
        }

        [Fact]
        public async Task GeneratesThePostMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("post(", result);
        }

        [Fact]
        public async Task GeneratesThePatchMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("patch(", result);
        }

        [Fact]
        public async Task GeneratesThePutMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("put(", result);
        }

        [Fact]
        public async Task GeneratesTheDeleteMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("delete(", result);
            Assert.DoesNotContain("result =", result);
        }

        [Fact]
        public async Task WritesTheRequestPayloadAsync()
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
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new User()", result);
            Assert.Contains("setAccountEnabled(true);", result);
            Assert.Contains("setPasswordProfile(passwordProfile);", result);
            Assert.Contains("setDisplayName(\"displayName-value\");", result);
        }

        [Fact]
        public async Task WritesALongAndFindsAnActionAsync()
        {
            const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10L", result);
            Assert.Contains("setChainId", result);
            Assert.Contains("sendActivityNotificationPostRequestBody", result);
        }

        [Fact]
        public async Task WritesADoubleAsync()
        {
            const string userJsonObject = "{\r\n  \"minimumAttendeePercentage\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10d", result);
            Assert.Contains("setMinimumAttendeePercentage", result);
        }

        [Fact]
        public async Task GeneratesABinaryPayloadAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo")
            {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            requestPayload.Content.Headers.ContentType = new("application/octet-stream");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new ByteArrayInputStream", result);
        }

        [Fact]
        public async Task GeneratesABase64UrlPayloadAsync()
        {
            const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Base64.getDecoder().decode", result);
            Assert.Contains("setContentBytes", result);
        }

        [Fact]
        public async Task GeneratesADateTimeOffsetPayloadAsync()
        {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("OffsetDateTime.parse(", result);
            Assert.Contains("message.setReceivedDateTime(receivedDateTime);", result);
        }

        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalDataAsync()
        {
            const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new LinkedList", result);
            Assert.Contains("AdditionalData", result);
            Assert.Contains("members", result); // property name hasn't been changed
        }

        [Fact]
        public async Task GeneratesCorrectTypeForObjectsInAdditionalDataWithSpecifiedOdataTypeAsync()
        {
            var bodyContent = """
                {
                  "transferTarget": {
                    "endpointType": "default",
                    "identity": {
                        "phone": {
                          "@odata.type": "#microsoft.graph.identity",
                          "id": "+12345678901"
                        }
                    },
                    "languageId": "languageId-value",
                    "region": "region-value"
                  },
                  "clientContext": "9e90d1c1-f61e-43e7-9f75-d420159aae08"
                }
                """;
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{call-id}}/transfer")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new Identity()", result);
            Assert.Contains("phone.setOdataType(\"#microsoft.graph.identity\")", result);
            Assert.Contains("phone.setId(\"+12345678901\")", result);
            Assert.Contains("additionalData.put(\"phone\", phone);", result);
        }

        [Fact]
        public async Task GeneratesSelectQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("displayName", result);
            Assert.Contains("requestConfiguration ->", result);
            Assert.Contains("requestConfiguration.queryParameters.select", result);
        }

        [Fact]
        public async Task GeneratesCountBooleanQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("\"displayName\"", result);
            Assert.Contains("\"id\"", result);
            Assert.DoesNotContain("\"true\"", result);
            Assert.Contains("true", result);
        }

        [Fact]
        public async Task GeneratesSkipQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("\"10\"", result);
            Assert.Contains("10", result);
        }

        [Fact]
        public async Task GeneratesSelectExpandQueryParametersAsync()///
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("Select", result);
        }

        [Fact]
        public async Task GeneratesRequestHeadersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.headers.add(\"ConsistencyLevel\", \"eventual\");", result);
            Assert.Contains("requestConfiguration ->", result);
        }

        [Fact]
        public async Task GeneratesFilterParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.queryParameters.count", result);
            Assert.Contains("requestConfiguration.queryParameters.filter", result);
            Assert.Contains("requestConfiguration.queryParameters.select", result);
            Assert.Contains("requestConfiguration.queryParameters.orderby", result);
        }

        [Fact]
        public async Task GeneratesFilterParametersWithSpecialCharactersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=imAddresses/any(i:i eq 'admin@contoso.com')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.queryParameters.filter", result);
            Assert.Contains("imAddresses/any(i:i eq 'admin@contoso.com')", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithDeltaAndSkipTokenAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/calendarView/delta?$skiptoken=R0usmcCM996atia_s");
            requestPayload.Headers.Add("Prefer", "odata.maxpagesize=2");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("DeltaGetResponse result = deltaRequestBuilder.get(", result);
            Assert.Contains("DeltaRequestBuilder deltaRequestBuilder = new com.microsoft.graph.users.item.calendarview.delta.DeltaRequestBuilder(", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOptionWithANDLogicalConjunctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:di\" AND \"displayName:al\"");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.queryParameters.search", result);
            Assert.Contains("requestConfiguration.queryParameters.search = \"\\\"displayName:di\\\" AND \\\"displayName:al\\\"\";", result);
        }

        [Fact]
        public async Task HandlesOdataTypeWhenGeneratingAsync()
        {
            var sampleJson = @"
                {
                ""@odata.type"": ""#microsoft.graph.socialIdentityProvider"",
                ""displayName"": ""Login with Amazon"",
                ""identityProviderType"": ""Amazon"",
                ""clientId"": ""56433757-cadd-4135-8431-2c9e3fd68ae8"",
                ""clientSecret"": ""000000000000""
                }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identity/identityProviders")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("setOdataType(\"#microsoft.graph.socialIdentityProvider\")", result);
            Assert.Contains("new SocialIdentityProvider", result);// ensure the derived type is used
        }

        [Fact]
        public async Task HandlesOdataReferenceSegmentsInUrlAsync()
        {
            var sampleJson = @"
                {
                ""@odata.id"": ""https://graph.microsoft.com/beta/users/alexd@contoso.com""
                }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups/id/acceptedSenders/$ref")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".acceptedSenders().ref().post(", result);
        }

        [Fact]
        public async Task GenerateSnippetsWithArrayNestingAsync()
        {
            var eventData = @"
             {
               ""subject"": ""Let's go for lunch"",
                ""body"": {
                    ""contentType"": ""Html"",
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
                    ""displayName"": null
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

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/events")
            {
                Content = new StringContent(eventData, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("setContentType(BodyType.Html)", result);
            Assert.Contains("new LinkedList<Attendee>()", result);
            Assert.Contains("setAttendees(attendees)", result);
            Assert.Contains("setStart(start)", result);
            Assert.Contains("setDisplayName(null)", result);
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
                        ""timeSlots"": [
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
                    ""meetingDuration"": ""PT1H""
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("meetingDuration = PeriodAndDuration.ofDuration(Duration.parse(\"PT1H\"));", result);
            Assert.Contains("setMeetingDuration(meetingDuration)", result);
            Assert.Contains("setIsRequired(false)", result);
            Assert.Contains("setLocationEmailAddress(\"conf32room1368@imgeek.onmicrosoft.com\")", result);
        }

        [Theory]
        [InlineData("sendMail")]
        [InlineData("microsoft.graph.sendMail")]
        public async Task FullyQualifiesActionRequestBodyTypeAsync(string sendMailString)
        {
            var bodyContent = @"{
                    ""message"": {
                    ""subject"": ""Meet for lunch?"",
                    ""body"": {
                        ""contentType"": ""Text"",
                        ""content"": ""The new cafeteria is open.""
                    },
                    ""toRecipients"": [
                    {
                        ""emailAddress"": {
                            ""address"": ""fannyd@contoso.onmicrosoft.com""
                        }
                    }
                    ],
                    ""ccRecipients"": [
                    {
                        ""emailAddress"": {
                            ""address"": ""danas@contoso.onmicrosoft.com""
                        }
                    }
                    ]
                },
                ""saveToSentItems"": ""false""
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/{{id}}/{sendMailString}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.users().byUserId(\"{user-id}\").sendMail().post(sendMailPostRequestBody);", result);
            Assert.Contains("SendMailPostRequestBody sendMailPostRequestBody = new com.microsoft.graph.users.item.sendmail.SendMailPostRequestBody()", result);
            Assert.Contains("toRecipients = new LinkedList<Recipient>", result);
        }

        [Fact]
        public async Task TypeArgumentsForListArePlacedCorrectlyAsync()
        {
            var bodyContent = @"{
                ""businessPhones"": [
                    ""+1 425 555 0109""
                        ],
                    ""officeLocation"": ""18/2111""
                }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/users/{{id}}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new LinkedList<String>", result);
        }

        [Fact]
        public async Task ModelsInNestedNamespacesAreDisambiguatedAsync()
        {
            var bodyContent = @"{
                ""id"": ""1431b9c38ee647f6a"",
                ""type"": ""externalGroup"",
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/external/connections/contosohr/groups/31bea3d537902000/members")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new com.microsoft.graph.models.externalconnectors.Identity", result);
            Assert.Contains("setType(com.microsoft.graph.models.externalconnectors.IdentityType.ExternalGroup)", result);
        }

        [Fact]
        public async Task ModelSubNamspacesAreExplicitlyTypedAsync()
        {
            var bodyContent = """
                {
                    "classification": "TruePositive",
                    "determination": "MultiStagedAttack",
                    "customTags": [
                      "Demo"
                    ]
                }
                """;
            
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/security/incidents/{{id}}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("com.microsoft.graph.models.security.Incident result = ", result);
        }

        [Fact]
        public async Task CorrectlyGeneratesEnumMemberAsync()
        {
            var bodyContent = @"{
                ""id"": ""SHPR_eeab4fb1-20e5-48ca-ad9b-98119d94bee7"",
                ""@odata.etag"": ""1a371e53-f0a6-4327-a1ee-e3c56e4b38aa"",
                ""availability"": [
                {
                    ""recurrence"": {
                        ""pattern"": {
                            ""type"": ""Weekly"",
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

            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/users/871dbd5c-3a6a-4392-bfe1-042452793a50/settings/shiftPreferences")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("setType(RecurrencePatternType.Weekly)", result);
            Assert.Contains("setType(RecurrenceRangeType.NoEnd)", result);
        }

        [Fact]
        public async Task CorrectlyGeneratesMultipleFlagsEnumMembersAsync()
        {
            var bodyContent = """
                {
                  "@odata.type": "#microsoft.graph.windows10GeneralConfiguration",
                  "description": "Description value",
                  "displayName": "Display Name value",
                  "version": 7,
                  "enterpriseCloudPrintDiscoveryEndPoint": "Enterprise Cloud Print Discovery End Point value",
                  "enterpriseCloudPrintOAuthAuthority": "Enterprise Cloud Print OAuth Authority value",
                  "enterpriseCloudPrintOAuthClientIdentifier": "Enterprise Cloud Print OAuth Client Identifier value",
                  "enterpriseCloudPrintResourceIdentifier": "Enterprise Cloud Print Resource Identifier value",
                  "enterpriseCloudPrintDiscoveryMaxLimit": 5,
                  "enterpriseCloudPrintMopriaDiscoveryResourceIdentifier": "Enterprise Cloud Print Mopria Discovery Resource Identifier value",
                  "searchBlockDiacritics": true,
                  "searchDisableAutoLanguageDetection": true,
                  "searchDisableIndexingEncryptedItems": true,
                  "searchEnableRemoteQueries": true,
                  "searchDisableIndexerBackoff": true,
                  "searchDisableIndexingRemovableDrive": true,
                  "searchEnableAutomaticIndexSizeManangement": true,
                  "diagnosticsDataSubmissionMode": "none",
                  "oneDriveDisableFileSync": true,
                  "smartScreenEnableAppInstallControl": true,
                  "personalizationDesktopImageUrl": "https://example.com/personalizationDesktopImageUrl/",
                  "personalizationLockScreenImageUrl": "https://example.com/personalizationLockScreenImageUrl/",
                  "bluetoothAllowedServices": [
                    "Bluetooth Allowed Services value"
                  ],
                  "bluetoothBlockAdvertising": true,
                  "bluetoothBlockDiscoverableMode": true,
                  "bluetoothBlockPrePairing": true,
                  "edgeBlockAutofill": true,
                  "edgeBlocked": true,
                  "edgeCookiePolicy": "allow",
                  "edgeBlockDeveloperTools": true,
                  "edgeBlockSendingDoNotTrackHeader": true,
                  "edgeBlockExtensions": true,
                  "edgeBlockInPrivateBrowsing": true,
                  "edgeBlockJavaScript": true,
                  "edgeBlockPasswordManager": true,
                  "edgeBlockAddressBarDropdown": true,
                  "edgeBlockCompatibilityList": true,
                  "edgeClearBrowsingDataOnExit": true,
                  "edgeAllowStartPagesModification": true,
                  "edgeDisableFirstRunPage": true,
                  "edgeBlockLiveTileDataCollection": true,
                  "edgeSyncFavoritesWithInternetExplorer": true,
                  "cellularBlockDataWhenRoaming": true,
                  "cellularBlockVpn": true,
                  "cellularBlockVpnWhenRoaming": true,
                  "defenderRequireRealTimeMonitoring": true,
                  "defenderRequireBehaviorMonitoring": true,
                  "defenderRequireNetworkInspectionSystem": true,
                  "defenderScanDownloads": true,
                  "defenderScanScriptsLoadedInInternetExplorer": true,
                  "defenderBlockEndUserAccess": true,
                  "defenderSignatureUpdateIntervalInHours": 6,
                  "defenderMonitorFileActivity": "disable",
                  "defenderDaysBeforeDeletingQuarantinedMalware": 12,
                  "defenderScanMaxCpu": 2,
                  "defenderScanArchiveFiles": true,
                  "defenderScanIncomingMail": true,
                  "defenderScanRemovableDrivesDuringFullScan": true,
                  "defenderScanMappedNetworkDrivesDuringFullScan": true,
                  "defenderScanNetworkFiles": true,
                  "defenderRequireCloudProtection": true,
                  "defenderCloudBlockLevel": "high",
                  "defenderPromptForSampleSubmission": "alwaysPrompt",
                  "defenderScheduledQuickScanTime": "11:58:49.3840000",
                  "defenderScanType": "disabled",
                  "defenderSystemScanSchedule": "everyday",
                  "defenderScheduledScanTime": "11:59:10.9990000",
                  "defenderDetectedMalwareActions": {
                    "@odata.type": "microsoft.graph.defenderDetectedMalwareActions",
                    "lowSeverity": "clean",
                    "moderateSeverity": "clean",
                    "highSeverity": "clean",
                    "severeSeverity": "clean"
                  },
                  "defenderFileExtensionsToExclude": [
                    "Defender File Extensions To Exclude value"
                  ],
                  "defenderFilesAndFoldersToExclude": [
                    "Defender Files And Folders To Exclude value"
                  ],
                  "defenderProcessesToExclude": [
                    "Defender Processes To Exclude value"
                  ],
                  "lockScreenAllowTimeoutConfiguration": true,
                  "lockScreenBlockActionCenterNotifications": true,
                  "lockScreenBlockCortana": true,
                  "lockScreenBlockToastNotifications": true,
                  "lockScreenTimeoutInSeconds": 10,
                  "passwordBlockSimple": true,
                  "passwordExpirationDays": 6,
                  "passwordMinimumLength": 5,
                  "passwordMinutesOfInactivityBeforeScreenTimeout": 14,
                  "passwordMinimumCharacterSetCount": 0,
                  "passwordPreviousPasswordBlockCount": 2,
                  "passwordRequired": true,
                  "passwordRequireWhenResumeFromIdleState": true,
                  "passwordRequiredType": "alphanumeric",
                  "passwordSignInFailureCountBeforeFactoryReset": 12,
                  "privacyAdvertisingId": "blocked",
                  "privacyAutoAcceptPairingAndConsentPrompts": true,
                  "privacyBlockInputPersonalization": true,
                  "startBlockUnpinningAppsFromTaskbar": true,
                  "startMenuAppListVisibility": "collapse, remove, userDEFINED ",
                  "startMenuHideChangeAccountSettings": true,
                  "startMenuHideFrequentlyUsedApps": true,
                  "startMenuHideHibernate": true,
                  "startMenuHideLock": true,
                  "startMenuHidePowerButton": true,
                  "startMenuHideRecentJumpLists": true,
                  "startMenuHideRecentlyAddedApps": true,
                  "startMenuHideRestartOptions": true,
                  "startMenuHideShutDown": true,
                  "startMenuHideSignOut": true,
                  "startMenuHideSleep": true,
                  "startMenuHideSwitchAccount": true,
                  "startMenuHideUserTile": true,
                  "startMenuLayoutEdgeAssetsXml": "c3RhcnRNZW51TGF5b3V0RWRnZUFzc2V0c1htbA==",
                  "startMenuLayoutXml": "c3RhcnRNZW51TGF5b3V0WG1s",
                  "startMenuMode": "fullScreen",
                  "startMenuPinnedFolderDocuments": "hide",
                  "startMenuPinnedFolderDownloads": "hide",
                  "startMenuPinnedFolderFileExplorer": "hide",
                  "startMenuPinnedFolderHomeGroup": "hide",
                  "startMenuPinnedFolderMusic": "hide",
                  "startMenuPinnedFolderNetwork": "hide",
                  "startMenuPinnedFolderPersonalFolder": "hide",
                  "startMenuPinnedFolderPictures": "hide",
                  "startMenuPinnedFolderSettings": "hide",
                  "startMenuPinnedFolderVideos": "hide",
                  "settingsBlockSettingsApp": true,
                  "settingsBlockSystemPage": true,
                  "settingsBlockDevicesPage": true,
                  "settingsBlockNetworkInternetPage": true,
                  "settingsBlockPersonalizationPage": true,
                  "settingsBlockAccountsPage": true,
                  "settingsBlockTimeLanguagePage": true,
                  "settingsBlockEaseOfAccessPage": true,
                  "settingsBlockPrivacyPage": true,
                  "settingsBlockUpdateSecurityPage": true,
                  "settingsBlockAppsPage": true,
                  "settingsBlockGamingPage": true,
                  "windowsSpotlightBlockConsumerSpecificFeatures": true,
                  "windowsSpotlightBlocked": true,
                  "windowsSpotlightBlockOnActionCenter": true,
                  "windowsSpotlightBlockTailoredExperiences": true,
                  "windowsSpotlightBlockThirdPartyNotifications": true,
                  "windowsSpotlightBlockWelcomeExperience": true,
                  "windowsSpotlightBlockWindowsTips": true,
                  "windowsSpotlightConfigureOnLockScreen": "disabled",
                  "networkProxyApplySettingsDeviceWide": true,
                  "networkProxyDisableAutoDetect": true,
                  "networkProxyAutomaticConfigurationUrl": "https://example.com/networkProxyAutomaticConfigurationUrl/",
                  "networkProxyServer": {
                    "@odata.type": "microsoft.graph.windows10NetworkProxyServer",
                    "address": "Address value",
                    "exceptions": [
                      "Exceptions value"
                    ],
                    "useForLocalAddresses": true
                  },
                  "accountsBlockAddingNonMicrosoftAccountEmail": true,
                  "antiTheftModeBlocked": true,
                  "bluetoothBlocked": true,
                  "cameraBlocked": true,
                  "connectedDevicesServiceBlocked": true,
                  "certificatesBlockManualRootCertificateInstallation": true,
                  "copyPasteBlocked": true,
                  "cortanaBlocked": true,
                  "deviceManagementBlockFactoryResetOnMobile": true,
                  "deviceManagementBlockManualUnenroll": true,
                  "safeSearchFilter": "strict",
                  "edgeBlockPopups": true,
                  "edgeBlockSearchSuggestions": true,
                  "edgeBlockSendingIntranetTrafficToInternetExplorer": true,
                  "edgeSendIntranetTrafficToInternetExplorer": true,
                  "edgeRequireSmartScreen": true,
                  "edgeEnterpriseModeSiteListLocation": "Edge Enterprise Mode Site List Location value",
                  "edgeFirstRunUrl": "https://example.com/edgeFirstRunUrl/",
                  "edgeSearchEngine": {
                    "@odata.type": "microsoft.graph.edgeSearchEngineBase"
                  },
                  "edgeHomepageUrls": [
                    "Edge Homepage Urls value"
                  ],
                  "edgeBlockAccessToAboutFlags": true,
                  "smartScreenBlockPromptOverride": true,
                  "smartScreenBlockPromptOverrideForFiles": true,
                  "webRtcBlockLocalhostIpAddress": true,
                  "internetSharingBlocked": true,
                  "settingsBlockAddProvisioningPackage": true,
                  "settingsBlockRemoveProvisioningPackage": true,
                  "settingsBlockChangeSystemTime": true,
                  "settingsBlockEditDeviceName": true,
                  "settingsBlockChangeRegion": true,
                  "settingsBlockChangeLanguage": true,
                  "settingsBlockChangePowerSleep": true,
                  "locationServicesBlocked": true,
                  "microsoftAccountBlocked": true,
                  "microsoftAccountBlockSettingsSync": true,
                  "nfcBlocked": true,
                  "resetProtectionModeBlocked": true,
                  "screenCaptureBlocked": true,
                  "storageBlockRemovableStorage": true,
                  "storageRequireMobileDeviceEncryption": true,
                  "usbBlocked": true,
                  "voiceRecordingBlocked": true,
                  "wiFiBlockAutomaticConnectHotspots": true,
                  "wiFiBlocked": true,
                  "wiFiBlockManualConfiguration": true,
                  "wiFiScanInterval": 0,
                  "wirelessDisplayBlockProjectionToThisDevice": true,
                  "wirelessDisplayBlockUserInputFromReceiver": true,
                  "wirelessDisplayRequirePinForPairing": true,
                  "windowsStoreBlocked": true,
                  "appsAllowTrustedAppsSideloading": "blocked",
                  "windowsStoreBlockAutoUpdate": true,
                  "developerUnlockSetting": "blocked",
                  "sharedUserAppDataAllowed": true,
                  "appsBlockWindowsStoreOriginatedApps": true,
                  "windowsStoreEnablePrivateStoreOnly": true,
                  "storageRestrictAppDataToSystemVolume": true,
                  "storageRestrictAppInstallToSystemVolume": true,
                  "gameDvrBlocked": true,
                  "experienceBlockDeviceDiscovery": true,
                  "experienceBlockErrorDialogWhenNoSIM": true,
                  "experienceBlockTaskSwitcher": true,
                  "logonBlockFastUserSwitching": true,
                  "tenantLockdownRequireNetworkDuringOutOfBoxExperience": true
                }
                """;
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/deviceManagement/deviceConfigurations")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains($"setStartMenuAppListVisibility(EnumSet.of(WindowsStartMenuAppListVisibilityType.Collapse, WindowsStartMenuAppListVisibilityType.Remove, WindowsStartMenuAppListVisibilityType.UserDefined))", result);
        }

        [Fact]
        public async Task CorrectlyAddsEnumsToEnumListAsync()
        {
            var bodyContent = """
                                {
                  "allowedCombinations": [
                      "password, voice"
                  ]
                }
                """;
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/policies/authenticationStrengthPolicies/{{policy-id}}/updateAllowedCombinations")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("allowedCombinations.add(AuthenticationMethodModes.Password);", result);
            Assert.Contains("allowedCombinations.add(AuthenticationMethodModes.Voice);", result);
        }

        [Fact]
        public async Task CorrectlyPicksSingleEnumWhenPayloadDescribesMultipleButRequestOnlyAcceptsOneAsync()
        {
            var bodyContent = """
                                {
                  "clientContext": "clientContext-value",
                  "status": "notRecording | recording | failed"
                }
                """;
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{id}}/updateRecordingStatus")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("setStatus(RecordingStatus.NotRecording)", result);
        }

        [Fact]
        public async Task CorrectlyOptionalRequestBodyParameterAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{id}}/archive");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.teams().byTeamId(\"{team-id}\").archive().post(null);", result);
        }

        [Fact]
        public async Task CorrectlyEvaluatesDatePropertyTypeRequestBodyParameterAsync()
        {
            var bodyContent = @"{
                ""subject"": ""Let's go for lunch"",
                ""recurrence"": {
                    ""range"": {
                        ""type"": ""endDate"",
                        ""startDate"": ""2017-09-04"",
                        ""endDate"": ""2017-12-31""
                    }
                }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/events")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("startDate = LocalDate.parse(\"2017-09-04\");", result);
            Assert.Contains("endDate = LocalDate.parse(\"2017-12-31\");", result);
            Assert.Contains("setStartDate(startDate)", result);
            Assert.Contains("setEndDate(endDate)", result);
        }

        [Fact]
        public async Task CorrectlyEvaluatesOdataActionRequestBodyParameterAsync()
        {
            var bodyContent = @"{
                ""keyCredential"": {
                        ""type"": ""AsymmetricX509Cert"",
                        ""usage"": ""Verify"",
                        ""key"": ""MIIDYDCCAki...""
                    },
                    ""passwordCredential"": null,
                    ""proof"":""eyJ0eXAiOiJ...""
                }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{id}}/addKey")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("AddKeyPostRequestBody addKeyPostRequestBody = new com.microsoft.graph.applications.item.addkey.AddKeyPostRequestBody()", result);
        }

        [Fact]
        public async Task CorrectlyEvaluatesGuidInRequestBodyParameterAsync()
        {
            var bodyContent = @"{
                  ""principalId"": ""cde330e5-2150-4c11-9c5b-14bfdc948c79"",
                  ""resourceId"": ""8e881353-1735-45af-af21-ee1344582a4d"",
                  ""appRoleId"": ""00000000-0000-0000-0000-000000000000""
                }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users/{{id}}/appRoleAssignments")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("UUID.fromString(\"cde330e5-2150-4c11-9c5b-14bfdc948c79\")", result);
            Assert.Contains("UUID.fromString(\"8e881353-1735-45af-af21-ee1344582a4d\")", result);
            Assert.Contains("UUID.fromString(\"00000000-0000-0000-0000-000000000000\")", result);
        }

        [Fact]
        public async Task DefaultsEnumIfNoneProvidedAsync()
        {
            var bodyContent = @"{
                ""subject"": ""subject-value"",
                ""body"": {
                ""contentType"": """",
                ""content"": ""content-value""
                },
                ""inferenceClassification"": ""other""
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{id}}")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("setContentType(BodyType.Text)", result);
        }

        [Fact]
        public async Task HandlesEmptyCollectionAsync()
        {
            var bodyContent = @"{
                ""defaultUserRolePermissions"": {
                ""permissionGrantPoliciesAssigned"": []
                }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/policies/authorizationPolicy")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("permissionGrantPoliciesAssigned = new LinkedList<String>", result);
        }

        [Fact]
        public async Task CorrectlyHandlesOdataFunctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/delta?$select=displayName,jobTitle,mobilePhone");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.users().delta().get(", result);
            Assert.Contains("requestConfiguration.queryParameters.select = new String []{\"displayName\", \"jobTitle\", \"mobilePhone\"};", result);
        }

        [Fact]
        public async Task CorrectlyHandlesDateTimeOffsetInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getUserArchivedPrintJobs(userId='{{id}}',startDateTime=<timestamp>,endDateTime=<timestamp>)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.reports().getUserArchivedPrintJobsWithUserIdWithStartDateTimeWithEndDateTime(OffsetDateTime.parse(\"{endDateTime}\"), OffsetDateTime.parse(\"{startDateTime}\"), \"{userId}\").get();", result);
        }

        [Fact]
        public async Task CorrectlyHandlesNumberInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/cell(row=<row>,column=<column>)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").workbook().worksheets().byWorkbookWorksheetId(\"{workbookWorksheet-id}\").cellWithRowWithColumn(1, 1).get()", result);
        }

        [Fact]
        public async Task CorrectlyHandlesDateInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getYammerGroupsActivityDetail(date='2018-03-05')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.reports().getYammerGroupsActivityDetailWithDate(LocalDate.parse(\"{date}\")).get()", result);
        }

        [Fact]
        public async Task CorrectlyHandlesDateInUrl2Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/communications/callRecords/getPstnCalls(fromDateTime=2019-11-01,toDateTime=2019-12-01)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.communications().callRecords().microsoftGraphCallRecordsGetPstnCallsWithFromDateTimeWithToDateTime(OffsetDateTime.parse(\"{fromDateTime}\"), OffsetDateTime.parse(\"{toDateTime}\")).get()", result);
        }

        [Fact]
        public async Task CorrectlyHandlesEnumInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityGovernance/appConsent/appConsentRequests/filterByCurrentUser(on='reviewer')?$filter=userConsentRequests/any(u:u/status eq 'InProgress')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.identityGovernance().appConsent().appConsentRequests().filterByCurrentUserWithOn(\"reviewer\").get", result);
        }

        [Fact]
        public async Task GeneratesObjectsInArrayAsync()
        {
            var sampleJson = @"
            {
            ""addLicenses"": [
                {
                ""disabledPlans"": [ ""11b0131d-43c8-4bbb-b2c8-e80f9a50834a"" ],
                ""skuId"": ""45715bb8-13f9-4bf6-927f-ef96c102d394""
                }
            ],
            ""removeLicenses"": [ ""bea13e0c-3828-4daa-a392-28af7ff61a0f"" ]
            }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/assignLicense")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("com.microsoft.graph.users.item.assignlicense.AssignLicensePostRequestBody assignLicensePostRequestBody = new com.microsoft.graph.users.item.assignlicense.AssignLicensePostRequestBody();", result);
            Assert.Contains("LinkedList<UUID> disabledPlans = new LinkedList<UUID>", result);
            Assert.Contains("LinkedList<UUID> removeLicenses = new LinkedList<UUID>", result);
            Assert.Contains("UUID.fromString(\"bea13e0c-3828-4daa-a392-28af7ff61a0f\")", result);
        }

        [Fact]
        public async Task GeneratesCorrectCollectionTypeAndDerivedInstancesAsync()
        {
            var sampleJson = @"{
              ""message"": {
                ""subject"": ""Meet for lunch?"",
                ""body"": {
                  ""contentType"": ""Text"",
                  ""content"": ""The new cafeteria is open.""
                },
                ""toRecipients"": [
                  {
                    ""emailAddress"": {
                      ""address"": ""meganb@contoso.onmicrosoft.com""
                    }
                  }
                ],
                ""attachments"": [
                  {
                    ""@odata.type"": ""#microsoft.graph.fileAttachment"",
                    ""name"": ""attachment.txt"",
                    ""contentType"": ""text/plain"",
                    ""contentBytes"": ""SGVsbG8gV29ybGQh""
                  }
                ]
              }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("SendMailPostRequestBody sendMailPostRequestBody = new com.microsoft.graph.users.item.sendmail.SendMailPostRequestBody()", result);
            Assert.Contains("LinkedList<Attachment> attachments = new LinkedList<Attachment>", result);// Collection defines Base type
            Assert.Contains("new FileAttachment", result);// Individual items are derived types
            Assert.Contains("byte[] contentBytes = Base64.getDecoder().decode(\"SGVsbG8gV29ybGQh\")", result);
        }

        [Fact]
        public async Task GeneratesPropertiesWithSpecialCharactersAsync()
        {
            var sampleJson = @"{
              ""@odata.type"": ""#microsoft.graph.managedIOSLobApp"",
              ""displayName"": ""Display Name value"",
              ""description"": ""Description value"",
              ""publisher"": ""Publisher value"",
              ""largeIcon"": {
                ""@odata.type"": ""microsoft.graph.mimeContent"",
                ""type"": ""Type value"",
                ""value"": ""dmFsdWU=""
              },
              ""isFeatured"": true,
              ""privacyInformationUrl"": ""https://example.com/privacyInformationUrl/"",
              ""informationUrl"": ""https://example.com/informationUrl/"",
              ""owner"": ""Owner value"",
              ""developer"": ""Developer value"",
              ""notes"": ""Notes value"",
              ""uploadState"": 11,
              ""publishingState"": ""processing"",
              ""isAssigned"": true,
              ""roleScopeTagIds"": [
                ""Role Scope Tag Ids value""
              ],
              ""dependentAppCount"": 1,
              ""supersedingAppCount"": 3,
              ""supersededAppCount"": 2,
              ""appAvailability"": ""lineOfBusiness"",
              ""version"": ""Version value"",
              ""committedContentVersion"": ""Committed Content Version value"",
              ""fileName"": ""File Name value"",
              ""size"": 4,
              ""bundleId"": ""Bundle Id value"",
              ""applicableDeviceType"": {
                ""@odata.type"": ""microsoft.graph.iosDeviceType"",
                ""iPad"": true,
                ""iPhoneAndIPod"": true
              },
              ""minimumSupportedOperatingSystem"": {
                ""@odata.type"": ""microsoft.graph.iosMinimumOperatingSystem"",
                ""v8_0"": true,
                ""v9_0"": true,
                ""v10_0"": true,
                ""v11_0"": true,
                ""v12_0"": true,
                ""v13_0"": true,
                ""v14_0"": true,
                ""v15_0"": true,
                ""v16_0"": true
              },
              ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-08:00"",
              ""versionNumber"": ""Version Number value"",
              ""buildNumber"": ""Build Number value"",
              ""identityVersion"": ""Identity Version value""
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootBetaUrl}/deviceAppManagement/mobileApps/{{mobileAppId}}")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("IosMinimumOperatingSystem minimumSupportedOperatingSystem = new IosMinimumOperatingSystem", result);
            Assert.Contains("setV80(true)", result); //assert property name is pascal case after the 'set' portion
        }

        [Fact]
        public async Task CorrectlyHandlesTypeFromInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/mailFolders/?includehiddenfolders=true");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.queryParameters.includeHiddenFolders = \"true\";", result);
        }

        [Fact]
        public async Task MatchesPathWithPathParameterAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/range(address='A1:B2')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("var result = graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").workbook().worksheets().byWorkbookWorksheetId(\"{workbookWorksheet-id}\").rangeWithAddress(\"{address}\").get()", result);
        }

        [Fact]
        public async Task MatchesPathAlternateKeysAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/applications(appId='46e6adf4-a9cf-4b60-9390-0ba6fb00bf6b')?$select=id,appId,displayName,requiredResourceAccess");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.applicationsWithAppId(\"{appId}\").get(", result);
        }

        [Theory]
        [InlineData("/me/drive/root/delta", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").delta().get()")]
        [InlineData("/groups/{group-id}/drive/items/{item-id}/children", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").children().get()")]
        [InlineData("/me/drive", "graphClient.me().drive().get()")]
        [InlineData("/sites/{site-id}/drive/items/{item-id}/children", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").children().get()")]
        [InlineData("/sites/{site-id}/drive/root/children", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").children().get()")]
        [InlineData("/users/{user-id}/drive/items/{item-id}/children", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").children().get()")]
        [InlineData("/me/drive/items/{item-id}/children", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").children().get()")]
        [InlineData("/drive/bundles", "graphClient.drives().byDriveId(\"{drive-id}\").bundles().get()")]
        [InlineData("/me/drive/special/documents", "graphClient.drives().byDriveId(\"{drive-id}\").special().byDriveItemId(\"{driveItem-id}\").get()")]
        [InlineData("/me/drive/root/search(q='Contoso%20Project')", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").searchWithQ(\"{q}\").get()")]
        [InlineData("/me/drive/items/{id}/workbook/application/calculate", "graphClient.drives().byDriveId(\"{drive-id}\").items().byDriveItemId(\"{driveItem-id}\").workbook().application().calculate()", "POST")]
        public async Task GeneratesSnippetWithRemappedDriveCallAsync(string inputPath, string expected, string method = "")
        {
            using var requestPayload = new HttpRequestMessage(string.IsNullOrEmpty(method) ? HttpMethod.Get : new HttpMethod(method), $"{ServiceRootUrl}{inputPath}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(expected, result);
        }
    }
}
