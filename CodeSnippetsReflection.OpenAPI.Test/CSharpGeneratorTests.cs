﻿using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class CSharpGeneratorTests {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private static OpenApiUrlTreeNode _v1TreeNode;
        private static async Task<OpenApiUrlTreeNode> GetV1TreeNode()
        {
            return _v1TreeNode ??= await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
        }
        private readonly CSharpGenerator _generator = new();
        [Fact]
        public async Task GeneratesTheCorrectFluentApiPath() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me.Messages", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentApiPathForIndexedCollections() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me.Messages[\"{message-id}\"]", result);
        }
        [Fact]
        public async Task GeneratesTheSnippetHeader() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("var graphClient = new GraphServiceClient(requestAdapter)", result);
        }
        [Fact]
        public async Task GeneratesTheGetMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("GetAsync", result);
            Assert.Contains("await", result);
        }
        [Fact]
        public async Task GeneratesThePostMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PostAsync", result);
        }
        [Fact]
        public async Task GeneratesThePatchMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PatchAsync", result);
        }
        [Fact]
        public async Task GeneratesThePutMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("PutAsync", result);
        }
        [Fact]
        public async Task GeneratesTheDeleteMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("DeleteAsync", result);
            Assert.DoesNotContain("var result =", result);
        }
        [Fact]
        public async Task WritesTheRequestPayload() {
            const string userJsonObject = "{\r\n  \"accountEnabled\": true,\r\n  " +
                                          "\"displayName\": \"displayName-value\",\r\n  " +
                                          "\"mailNickname\": \"mailNickname-value\",\r\n  " +
                                          "\"userPrincipalName\": \"upn-value@tenant-value.onmicrosoft.com\",\r\n " +
                                          " \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"password-value\"\r\n  }\r\n}";//nested passwordProfile Object

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new User", result);
            Assert.Contains("AccountEnabled = true,", result);
            Assert.Contains("PasswordProfile = new PasswordProfile", result);
            Assert.Contains("DisplayName = \"displayName-value\"", result);
        }
        [Fact]
        public async Task WritesALongAndFindsAnAction() {
            const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10L", result);
            Assert.Contains("SendActivityNotificationPostRequestBody", result);
            Assert.DoesNotContain("microsoft.graph", result);
        }
        [Fact]
        public async Task WritesADouble() {
            const string userJsonObject = "{\r\n  \"minimumAttendeePercentage\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10d", result);
        }
        [Fact]
        public async Task GeneratesABinaryPayload() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            requestPayload.Content.Headers.ContentType = new ("application/octet-stream");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new MemoryStream", result);
        }
        [Fact]
        public async Task GeneratesABase64UrlPayload() {
            const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Convert.FromBase64String", result);
        }
        [Fact]
        public async Task GeneratesADateTimeOffsetPayload() {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("DateTimeOffset.Parse", result);
        }
        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalData() {
            const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("new List", result);
            Assert.Contains("AdditionalData", result);
            Assert.Contains("members", result); // property name hasn't been changed
        }
        [Fact]
        public async Task GeneratesSelectQueryParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("displayName", result);
            Assert.Contains("(requestConfiguration) =>", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select", result);
        }
        [Fact]
        public async Task GeneratesCountBooleanQueryParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("displayName", result);
            Assert.DoesNotContain("\"true\"", result);
            Assert.Contains("true", result);
        }
        [Fact]
        public async Task GeneratesSkipQueryParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("\"10\"", result);
            Assert.Contains("10", result);
        }
        [Fact]
        public async Task GeneratesSelectExpandQueryParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("Select", result);
        }
        [Fact]
        public async Task GeneratesRequestHeaders() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.Headers.Add(\"ConsistencyLevel\", \"eventual\");", result);
            Assert.Contains("(requestConfiguration) =>", result);
        }
        [Fact]
        public async Task GeneratesFilterParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$filter=Department eq 'Finance'&$orderBy=displayName&$select=id,displayName,department");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestConfiguration.QueryParameters.Count", result);
            Assert.Contains("requestConfiguration.QueryParameters.Filter", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select", result);
            Assert.Contains("requestConfiguration.QueryParameters.Orderby", result);
        }
        [Fact]
        public async Task HandlesOdataTypeWhenGenerating() {
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("OdataType = \"#microsoft.graph.socialIdentityProvider\",", result);
        }
        [Fact]
        public async Task HandlesOdataReferenceSegmentsInUrl() {
            var sampleJson = @"
                {
                ""@odata.id"": ""https://graph.microsoft.com/beta/users/alexd@contoso.com""
                }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups/id/acceptedSenders/$ref"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".AcceptedSenders.Ref.PostAsync(requestBody);", result);
        }
        [Fact]
        public async Task GenerateSnippetsWithArrayNesting()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("ContentType = BodyType.Html", result);
            Assert.Contains("Attendees = new List<Attendee>", result);
            Assert.Contains("Start = new DateTimeTimeZone", result);
            Assert.Contains("DisplayName = null,", result);
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("MeetingDuration = TimeSpan.Parse(\"PT1H\")", result);
            Assert.Contains("IsRequired = false,", result);
            Assert.Contains("LocationEmailAddress = \"conf32room1368@imgeek.onmicrosoft.com\",", result);
        }
        
        [Theory]
        [InlineData("sendMail")]
        [InlineData("microsoft.graph.sendMail")]
        public async Task FullyQualifiesActionRequestBodyType(string sendMailString)
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Users[\"{user-id}\"].SendMail.PostAsync(requestBody);", result);
            Assert.Contains("var requestBody = new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody", result);
            Assert.Contains("ToRecipients = new List<Recipient>", result);
        }
        
        [Fact]
        public async Task TypeArgumentsForListArePlacedCorrectly()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("new List<string>", result);
        }
        
        [Fact]
        public async Task ModelsInNestedNamespacesAreDisambiguated()
        {
            var bodyContent = @"{
                ""id"": ""1431b9c38ee647f6a"",
                ""type"": ""externalGroup"",
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/external/connections/contosohr/groups/31bea3d537902000/members")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("new Microsoft.Graph.Models.ExternalConnectors.Identity", result);
            Assert.Contains("Type = Microsoft.Graph.Models.ExternalConnectors.IdentityType.ExternalGroup", result);
        }
        
        [Fact]
        public async Task ReplacesReservedTypeNames()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/directory/administrativeUnits/8a07f5a8-edc9-4847-bbf2-dde106594bf4/scopedRoleMembers");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            // Assert `Directory` is replaced with `DirectoryObject`
            Assert.Contains("await graphClient.Directory.AdministrativeUnits[\"{administrativeUnit-id}\"].ScopedRoleMembers.GetAsync()", result);
        }
        
        [Fact]
        public async Task CorrectlyGeneratesEnumMember()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("Type = RecurrencePatternType.Weekly,", result);
            Assert.Contains("Type = RecurrenceRangeType.NoEnd,", result);
        }
        
        [Fact]
        public async Task CorrectlyGeneratesMultipleFlagsEnumMembers()
        {
            var bodyContent = @"{
                ""clientContext"": ""clientContext-value"",
                ""status"": ""notRecorDing | recording , failed""
            }";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/communications/calls/{{id}}/updateRecordingStatus")
            {
                Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("Status = RecordingStatus.NotRecording | RecordingStatus.Recording | RecordingStatus.Failed", result);
        }
        
        [Fact]
        public async Task CorrectlyOptionalRequestBodyParameter()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{id}}/archive");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Teams[\"{team-id}\"].Archive.PostAsync(null);", result);
        }
        
        [Fact]
        public async Task CorrectlyEvaluatesDatePropertyTypeRequestBodyParameter()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("StartDate = new Date(DateTime.Parse(\"2017-09-04\")),", result);
            Assert.Contains("EndDate = new Date(DateTime.Parse(\"2017-12-31\")),", result);
        }
        
        [Fact]
        public async Task CorrectlyEvaluatesOdataActionRequestBodyParameter()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("var requestBody = new Microsoft.Graph.Applications.Item.AddKey.AddKeyPostRequestBody", result);
        }
        [Fact]
        public async Task CorrectlyEvaluatesGuidInRequestBodyParameter()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("Guid.Parse(\"cde330e5-2150-4c11-9c5b-14bfdc948c79\")", result);
            Assert.Contains("Guid.Parse(\"8e881353-1735-45af-af21-ee1344582a4d\")", result);
            Assert.Contains("Guid.Parse(\"00000000-0000-0000-0000-000000000000\")", result);
        }
        [Fact]
        public async Task CorrectlyHandlesOdataFunction()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/delta?$select=displayName,jobTitle,mobilePhone");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("await graphClient.Users.Delta.GetAsync", result);
            Assert.Contains("requestConfiguration.QueryParameters.Select = new string []{ \"displayName\",\"jobTitle\",\"mobilePhone\" };", result);
        }
    }
}
