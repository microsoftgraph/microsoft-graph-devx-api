using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class CSharpGeneratorTests : OpenApiSnippetGeneratorTestBase
    {
        private readonly CSharpGenerator _generator = new();

        [Fact]
        public async Task GeneratesTheCorrectFluentApiPathAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me.Messages", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentApiPath_2Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityGovernance/lifecycleWorkflows/workflows/{{workflowId}}/versions/{{workflowVersion-versionNumber}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Versions[2]", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentApiPathForIndexedCollectionsAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me.Messages[\"{message-id}\"]", result);
        }
        [Fact]
        public async Task GeneratesTheSnippetHeaderAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("var graphClient = new GraphServiceClient(requestAdapter)", result); // no initialization statement present
            Assert.Contains("// Code snippets are only available for the latest version. Current version is", result);// ensure version comment is present
            Assert.Contains("// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client", result);// ensure initialization guidance is present
        }
        [Fact]
        public async Task GeneratesTheGetMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("GetAsync", result);
            Assert.Contains("await", result);
        }
        [Fact]
        public async Task GeneratesThePostMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PostAsync", result);
        }
        [Fact]
        public async Task GeneratesThePatchMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PatchAsync", result);
        }
        [Fact]
        public async Task GeneratesThePutMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PutAsync", result);
        }
        [Fact]
        public async Task GeneratesTheDeleteMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("DeleteAsync", result);
            Assert.DoesNotContain("var result =", result);
        }
        [Fact]
        public async Task WritesTheRequestPayloadAsync() {
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
            Assert.Contains("new User", result);
            Assert.Contains("AccountEnabled = true,", result);
            Assert.Contains("PasswordProfile = new PasswordProfile", result);
            Assert.Contains("DisplayName = \"displayName-value\"", result);
        }
        [Fact]
        public async Task WritesALongAndFindsAnActionAsync() {
            const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10L", result);
            Assert.Contains("SendActivityNotificationPostRequestBody", result);
            Assert.DoesNotContain("microsoft.graph", result);
        }
        [Fact]
        public async Task WritesADoubleAsync() {
            const string userJsonObject = "{\r\n  \"minimumAttendeePercentage\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10d", result);
        }
        [Fact]
        public async Task GeneratesABinaryPayloadAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            requestPayload.Content.Headers.ContentType = new ("application/octet-stream");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new MemoryStream", result);
        }
        [Fact]
        public async Task GeneratesABase64UrlPayloadAsync() {
            const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Convert.FromBase64String", result);
        }
        [Fact]
        public async Task GeneratesADateTimeOffsetPayloadAsync() {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("DateTimeOffset.Parse", result);
        }
        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalDataAsync() {
            const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new List", result);
            Assert.Contains("AdditionalData", result);
            Assert.Contains("members", result); // property name hasn't been changed
        }
        [Fact]
        public async Task GeneratesSelectQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("displayName", result);
            Assert.Contains("(requestConfiguration) =>", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select", result);
        }
        [Fact]
        public async Task GeneratesCountBooleanQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("displayName", result);
            Assert.DoesNotContain("\"true\"", result);
            Assert.Contains("true", result);
        }
        [Fact]
        public async Task GeneratesSkipQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("\"10\"", result);
            Assert.Contains("10", result);
        }
        [Fact]
        public async Task GeneratesSelectExpandQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("Select", result);
        }
        [Fact]
        public async Task GeneratesRequestHeadersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.Headers.Add(\"ConsistencyLevel\", \"eventual\");", result);
            Assert.Contains("(requestConfiguration) =>", result);
        }
        [Fact]
        public async Task GeneratesFilterParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.QueryParameters.Count", result);
            Assert.Contains("requestConfiguration.QueryParameters.Filter", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select", result);
            Assert.Contains("requestConfiguration.QueryParameters.Orderby", result);
        }
        [Fact]
        public async Task GeneratesFilterParametersWithSpecialCharactersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=imAddresses/any(i:i eq 'admin@contoso.com')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.QueryParameters.Filter", result);
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
            Assert.Contains("var result = await graphClient.Me.CalendarView.Delta.WithUrl(\"", result);
            Assert.Contains("requestConfiguration.Headers.Add(\"Prefer\", \"odata.maxpagesize=2\");", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithIndexerDeltaAndSkipTokenAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/education/classes/72a7baec-c3e9-4213-a850-f62de0adad5f/assignments/delta?$skiptoken=U43TyYWKlRvJ6wWxZOfJvkp22nMqShRw9f-GxBtG2FDy9b1hMDaAJGdLb7n2fh1IdHoweKQs1czM4Ry1LVsNqwIFXftTcRHvgSCbcszvbJHEWDCO3QO7K7zwCM8DdXNepZOa1gqldecjIUM0NFRbGQoQ5yR6RmGnMgtko8TDMOyMH_yg1my82PTXA_t4Nj-DhMDZWvuNTd_lbLeTngc7mIJPMCR2gHN9CSKsW_kw850.UM9tUqwOu5Ln1pnxaP6KdMmfJHszGqY3EKPlQkOiyGs");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("var result = await graphClient.Education.Classes[\"{educationClass-id}\"].Assignments.Delta.WithUrl(\"", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOptionWithANDLogicalConjunctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:di\" AND \"displayName:al\"");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.QueryParameters.Search", result);
            Assert.Contains("requestConfiguration.QueryParameters.Search = \"\\\"displayName:di\\\" AND \\\"displayName:al\\\"\";", result);
        }

        [Theory]
        [InlineData("/users/some-id/mailFolders/delta","GetAsDeltaGetResponseAsync")]
        [InlineData("/appCatalogs/teamsApps?requiresReview=true","await graphClient.AppCatalogs.TeamsApps.PostAsync(", "POST")]
        [InlineData("/me/photos/48x48/$value","Content.GetAsync(")]
        public async Task ObsoleteGetAsyncReplacedWithGetAsLastNodeNameGetResponseAsync(string inputPath, string expectedSuffix, string method = "") {
            using var requestPayload = new HttpRequestMessage(string.IsNullOrEmpty(method) ? HttpMethod.Get : new HttpMethod(method), $"{ServiceRootUrl}{inputPath}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(expectedSuffix, result);
        }

        [Fact]
        public async Task HandlesOdataTypeWhenGeneratingAsync() {
            var sampleJson = @"
                {
                ""@odata.type"": ""#microsoft.graph.socialIdentityProvider"",
                ""displayName"": ""Login with Amazon"",
                ""identityProviderType"": ""Amazon"",
                ""clientId"": ""56433757-cadd-4135-8431-2c9e3fd68ae8"",
                ""clientSecret"": ""000000000000""
                }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identity/identityProviders"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("OdataType = \"#microsoft.graph.socialIdentityProvider\",", result);
            Assert.Contains("new SocialIdentityProvider", result);// ensure the derived type is used
        }
        [Fact]
        public async Task HandlesOdataReferenceSegmentsInUrlAsync() {
            var sampleJson = @"
                {
                ""@odata.id"": ""https://graph.microsoft.com/beta/users/alexd@contoso.com""
                }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups/id/acceptedSenders/$ref"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".AcceptedSenders.Ref.PostAsync(requestBody);", result);
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

            Assert.Contains("ContentType = BodyType.Html", result);
            Assert.Contains("Attendees = new List<Attendee>", result);
            Assert.Contains("Start = new DateTimeTimeZone", result);
            Assert.Contains("DisplayName = null,", result);
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
            Assert.Contains("MeetingDuration = TimeSpan.Parse(\"PT1H\")", result);
            Assert.Contains("IsRequired = false,", result);
            Assert.Contains("LocationEmailAddress = \"conf32room1368@imgeek.onmicrosoft.com\",", result);
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

            Assert.Contains("await graphClient.Users[\"{user-id}\"].SendMail.PostAsync(requestBody);", result);
            Assert.Contains("var requestBody = new SendMailPostRequestBody", result);
            Assert.Contains("using Microsoft.Graph.Users.Item.SendMail", result);
            Assert.Contains("ToRecipients = new List<Recipient>", result);
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

            Assert.Contains("new List<string>", result);
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

            Assert.Contains("new Identity", result);
            Assert.Contains("using Microsoft.Graph.Models.ExternalConnectors;", result);
            Assert.Contains("Type = IdentityType.ExternalGroup", result);
            Assert.Contains("using Microsoft.Graph.Models.ExternalConnectors;", result);
        }

        [Fact]
        public async Task ReplacesReservedTypeNamesAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/directory/administrativeUnits/8a07f5a8-edc9-4847-bbf2-dde106594bf4/scopedRoleMembers");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            // Assert `Directory` is replaced with `DirectoryObject`
            Assert.Contains("await graphClient.Directory.AdministrativeUnits[\"{administrativeUnit-id}\"].ScopedRoleMembers.GetAsync()", result);
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

            Assert.Contains("Type = RecurrencePatternType.Weekly,", result);
            Assert.Contains("Type = RecurrenceRangeType.NoEnd,", result);
        }

        [Fact]
        public async Task CorrectlyGeneratesMultipleFlagsEnumMembersAsync()
        {
            var bodyContent = @"{
                ""clientContext"": ""clientContext-value"",
                ""status"": ""notRecorDing | recording , failed""
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{id}}/updateRecordingStatus")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("Status = RecordingStatus.NotRecording | RecordingStatus.Recording | RecordingStatus.Failed", result);
        }

        [Fact]
        public async Task CorrectlyOptionalRequestBodyParameterAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{id}}/archive");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Teams[\"{team-id}\"].Archive.PostAsync(null);", result);
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

            Assert.Contains("StartDate = new Date(DateTime.Parse(\"2017-09-04\")),", result);
            Assert.Contains("EndDate = new Date(DateTime.Parse(\"2017-12-31\")),", result);
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

            Assert.Contains("var requestBody = new AddKeyPostRequestBody", result);
            Assert.Contains("using Microsoft.Graph.Applications.Item.AddKey;", result);
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

            Assert.Contains("Guid.Parse(\"cde330e5-2150-4c11-9c5b-14bfdc948c79\")", result);
            Assert.Contains("Guid.Parse(\"8e881353-1735-45af-af21-ee1344582a4d\")", result);
            Assert.Contains("Guid.Parse(\"00000000-0000-0000-0000-000000000000\")", result);
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
            Assert.Contains("ContentType = BodyType.Text,", result);
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
            Assert.Contains("PermissionGrantPoliciesAssigned = new List<string>", result);
        }
        [Fact]
        public async Task CorrectlyHandlesOdataFunctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/delta?$select=displayName,jobTitle,mobilePhone");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Users.Delta.GetAsDeltaGetResponseAsync", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select = new string []{ \"displayName\",\"jobTitle\",\"mobilePhone\" };", result);
        }

        [Fact]
        public async Task CorrectlyHandlesDateTimeOffsetInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getUserArchivedPrintJobs(userId='{{id}}',startDateTime=<timestamp>,endDateTime=<timestamp>)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Reports.GetUserArchivedPrintJobsWithUserIdWithStartDateTimeWithEndDateTime(DateTimeOffset.Parse(\"{endDateTime}\"),DateTimeOffset.Parse(\"{startDateTime}\"),\"{userId}\").GetAsGetUserArchivedPrintJobsWithUserIdWithStartDateTimeWithEndDateTimeGetResponseAsync()", result);
        }

        [Fact]
        public async Task CorrectlyHandlesNumberInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/cell(row=<row>,column=<column>)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Workbook.Worksheets[\"{workbookWorksheet-id}\"].CellWithRowWithColumn(1,1).GetAsync();",result);
        }
        [Fact]
        public async Task CorrectlyHandlesDateInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/reports/getYammerGroupsActivityDetail(date='2018-03-05')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Reports.GetYammerGroupsActivityDetailWithDate(new Date(DateTime.Parse(\"{date}\"))).GetAsync();", result);
        }
        [Fact]
        public async Task CorrectlyHandlesDateInUrl2Async()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/communications/callRecords/getPstnCalls(fromDateTime=2019-11-01,toDateTime=2019-12-01)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Communications.CallRecords.MicrosoftGraphCallRecordsGetPstnCallsWithFromDateTimeWithToDateTime(DateTimeOffset.Parse(\"{fromDateTime}\"),DateTimeOffset.Parse(\"{toDateTime}\")).GetAsGetPstnCallsWithFromDateTimeWithToDateTimeGetResponseAsync()", result);
        }
        [Fact]
        public async Task CorrectlyHandlesEnumInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityGovernance/appConsent/appConsentRequests/filterByCurrentUser(on='reviewer')?$filter=userConsentRequests/any(u:u/status eq 'InProgress')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.IdentityGovernance.AppConsent.AppConsentRequests.FilterByCurrentUserWithOn(\"reviewer\").GetAsFilterByCurrentUserWithOnGetResponseAsync", result);
        }
        [Fact]
        public async Task GeneratesObjectsInArrayAsync() {
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
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/assignLicense"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("var requestBody = new AssignLicensePostRequestBody", result);
            Assert.Contains("using Microsoft.Graph.Me.AssignLicense;", result);
            Assert.Contains("DisabledPlans = new List<Guid?>", result);
            Assert.Contains("RemoveLicenses = new List<Guid?>", result);
            Assert.Contains("Guid.Parse(\"bea13e0c-3828-4daa-a392-28af7ff61a0f\"),", result);
        }
        [Fact]
        public async Task GeneratesCorrectCollectionTypeAndDerivedInstancesAsync() {
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
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/sendMail"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("var requestBody = new SendMailPostRequestBody", result);
            Assert.Contains("using Microsoft.Graph.Me.SendMail;", result);
            Assert.Contains("Attachments = new List<Attachment>", result);// Collection defines Base type
            Assert.Contains("new FileAttachment", result);// Individual items are derived types
            Assert.Contains("ContentBytes = Convert.FromBase64String(\"SGVsbG8gV29ybGQh\"),", result);
        }
        [Fact]
        public async Task GenerateUntypedTypesInstancesInAdditionalDataAsync() {
            var sampleJson = @"{
                ""customSecurityAttributes"":
                {
                    ""Engineering"":
                    {
                        ""@odata.type"":""#Microsoft.DirectoryServices.CustomSecurityAttributeValue"",
                        ""ProjectDate"":null
                    }
                }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/users/{{id}}"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("using Microsoft.Kiota.Abstractions.Serialization;", result);//verify import
            Assert.Contains("\"projectDate\", new UntypedNull()", result); // ProjectDate  should be initialized with specified type.
        }

        [Fact]
        public async Task GenerateUntypedTypesInstancesInAdditionalData2Async()
        {
            var sampleJson = @"{
                ""allowedValues@delta"": [
                    {
                        ""id"": ""Baker"",
                        ""isActive"": false
                    },
                    {
                        ""id"": ""Skagit"",
                        ""isActive"": true
                    }
                ]
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootBetaUrl}/directory/customSecurityAttributeDefinitions/Engineering_Project")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("using Microsoft.Kiota.Abstractions.Serialization;", result);//verify import
            Assert.Contains("\"allowedValues@delta\" , new List<object>", result); 
            Assert.Contains("\"id\", new UntypedString(\"Baker\")", result);
            Assert.Contains("\"id\", new UntypedString(\"Skagit\")", result);
        }
        [Fact]
        public async Task GeneratesCorrectTypesInstancesInAdditionalDataAsync() {
            var sampleJson = @"{
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
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{id}}/transfer"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("\"phone\" , new Identity", result);//phone should be initialized with specified type.
        }
        [Fact]
        public async Task GeneratesPropertiesWithSpecialCharactersAsync() {
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
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootBetaUrl}/deviceAppManagement/mobileApps/{{mobileAppId}}"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("MinimumSupportedOperatingSystem = new IosMinimumOperatingSystem", result);
            Assert.Contains("V80 = true,", result);//Assert that the property was pascal cased
        }

        [Fact]
        public async Task GeneratesCorrectTypeInCollectionInitializerAsync() {
            var sampleJson = @"{
                ""workflow"":{
                    ""category"": ""joiner"",
                    ""description"": ""Configure new hire tasks for onboarding employees on their first day"",
                    ""displayName"": ""Global onboard new hire employee"",
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
                            ""offsetInDays"": 1
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
                            ""arguments"": []
                        }
                    ]
                }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identityGovernance/lifecycleWorkflows/workflows/{{workflowId}}/createNewVersion"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new List<TaskObject>", result);//Assert the type is escaped in the collection initializzer.
            Assert.Contains("using Microsoft.Graph.Models.IdentityGovernance", result);//Assert the type is escaped in the collection initializzer.
        }

        [Fact]
        public async Task CorrectlyHandlesTypeFromInUrlAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/mailFolders/?includehiddenfolders=true");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("requestConfiguration.QueryParameters.IncludeHiddenFolders = \"true\";", result);
        }

        [Fact]
        public async Task MatchesPathWithPathParameterAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/range(address='A1:B2')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("var result = await graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Workbook.Worksheets[\"{workbookWorksheet-id}\"].RangeWithAddress(\"{address}\").GetAsync()", result);
        }
        
        [Fact]
        public async Task UntypedNodeInWorkbooksGeneratesArrayAsync()
        {
            var sampleJson = "{\n  \"index\": 5,\n  \"values\": [\n    [1, 2, 3],\n    [4, 5, 6]\n  ]\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/tables/{{id|name}}/rows/add")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("using Microsoft.Kiota.Abstractions.Serialization;", result);//verify import
            Assert.Contains("var result = await graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Workbook.Tables[\"{workbookTable-id}\"].Rows.Add.PostAsync(requestBody);", result);
            Assert.Contains("new UntypedArray(new List<UntypedNode>", result);
            Assert.Contains("Values = new UntypedArray(new List<UntypedNode>", result);
        }

        [Fact]
        public async Task UntypedNodeInWorkbooksGeneratesObjectAsync()
        {
            var sampleJson = "{\r\n    \"lookupValue\":\"pear\",\r\n    \"tableArray\":{\"Address\":\"Sheet1!B2:C7\"},\r\n    \"colIndexNum\":2,\r\n    \"rangeLookup\":false\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/functions/vlookup")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("using Microsoft.Kiota.Abstractions.Serialization;", result);//verify import
            Assert.Contains("var result = await graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Workbook.Functions.Vlookup.PostAsync(requestBody);", result);
            Assert.Contains("LookupValue = new UntypedString(\"pear\"),", result);
            Assert.Contains("TableArray = new UntypedObject(new Dictionary<string, UntypedNode>", result);
            Assert.Contains("\"Address\", new UntypedString(\"Sheet1!B2:C7\")", result);
        }

        [Fact]
        public async Task MatchesPathAlternateKeysAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/applications(appId='46e6adf4-a9cf-4b60-9390-0ba6fb00bf6b')?$select=id,appId,displayName,requiredResourceAccess");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.ApplicationsWithAppId(\"{appId}\").GetAsync(", result);
        }
        [Theory]
        [InlineData("/me/drive/root/delta","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Delta.GetAsDeltaGetResponseAsync()")]
        [InlineData("/groups/{group-id}/drive/items/{item-id}/children","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Children.GetAsync()")]
        [InlineData("/me/drive","graphClient.Me.Drive.GetAsync()")]
        [InlineData("/sites/{site-id}/drive/items/{item-id}/children","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Children.GetAsync()")]
        [InlineData("/sites/{site-id}/drive/root/children","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Children.GetAsync()")]
        [InlineData("/users/{user-id}/drive/items/{item-id}/children","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Children.GetAsync()")]
        [InlineData("/me/drive/items/{item-id}/children","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Children.GetAsync()")]
        [InlineData("/drive/bundles","graphClient.Drives[\"{drive-id}\"].Bundles.GetAsync()")]
        [InlineData("/me/drive/special/documents","graphClient.Drives[\"{drive-id}\"].Special[\"{driveItem-id}\"].GetAsync()")]
        [InlineData("/me/drive/root/search(q='Contoso%20Project')","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].SearchWithQ(\"{q}\").GetAsSearchWithQGetResponseAsync()")]
        [InlineData("/me/drive/items/{id}/workbook/application/calculate","graphClient.Drives[\"{drive-id}\"].Items[\"{driveItem-id}\"].Workbook.Application.Calculate", "POST")]
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
