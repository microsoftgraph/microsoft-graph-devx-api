using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class TypeScriptGeneratorTest : OpenApiSnippetGeneratorTestBase
    {
        private readonly TypeScriptGenerator _generator = new();

        // check path correctness
        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.get();
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesClassWithDefaultBodyWhenSchemaNotPresentAsync()
        {
            const string userJsonObject = "{  \"decision\": \"Approve\",  \"justification\": \"All principals with access need continued access to the resource (Marketing Group) as all the principals are on the marketing team\",  \"resourceId\": \"a5c51e59-3fcd-4a37-87a1-835c0c21488a\"}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identityGovernance/accessReviews/definitions/e6cafba0-cbf0-4748-8868-0810c7f4cc06/instances/1234fba0-cbf0-6778-8868-9999c7f4cc06/batchRecordDecisions")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-identitygovernance"";
            import { BatchRecordDecisionsPostRequestBody } from ""@microsoft/msgraph-sdk-identitygovernance/identityGovernance/accessReviews/definitions/item/instances/item/batchRecordDecisions"";
            //other-imports

            const requestBody : BatchRecordDecisionsPostRequestBody = {
	            decision : ""Approve"",
	            justification : ""All principals with access need continued access to the resource (Marketing Group) as all the principals are on the marketing team"",
	            resourceId : ""a5c51e59-3fcd-4a37-87a1-835c0c21488a"",
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.identityGovernance.accessReviews.definitions.byAccessReviewScheduleDefinitionId(""accessReviewScheduleDefinition-id"").instances.byAccessReviewInstanceId(""accessReviewInstance-id"").batchRecordDecisions.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathForIndexedCollectionsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-users"";
            //other-imports

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.byMessageId(""message-id"").get();
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesTheSnippetInitializationDeclarationAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("// To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript", result);
        }

        [Fact]
        public async Task GeneratesTheGetMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-users"";
            //other-imports

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.get();
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePostMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
                const result = async () => {
	            await graphServiceClient.me.messages.post();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePatchMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-users"";
            //other-imports

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.byMessageId(""message-id"").patch();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePutMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("put", result);
        }

        [Fact]
        public async Task GeneratesTheDeleteMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-users"";
            //other-imports

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.byMessageId(""message-id"").delete();
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task WritesTheRequestPayloadAsync()
        {
            var sampleJson = @"
                        {
                            ""accountEnabled"": true,
                            ""displayName"": ""displayName-value"",
                            ""mailNickname"": ""mailNickname-value"",
                            ""userPrincipalName"": ""upn-value@tenant-value.onmicrosoft.com"",
                            "" passwordProfile"": {
                                ""forceChangePasswordNextSignIn"": true,
                                ""password"": ""password-value""
                            }
                        }
                        ";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
                //THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY
                // Dependencies
                import ""@microsoft/msgraph-sdk-users"";
                import { User } from ""@microsoft/msgraph-sdk/models"";
                //other-imports

                const requestBody : User = {
	                accountEnabled : true,
	                displayName : ""displayName-value"",
	                mailNickname : ""mailNickname-value"",
	                userPrincipalName : ""upn-value@tenant-value.onmicrosoft.com"",
	                additionalData : {
		                 passwordProfile : {
			                forceChangePasswordNextSignIn : true,
			                password : ""password-value"",
		                },
	                },
                };

                // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

                const result = async () => {
	                await graphServiceClient.users.post(requestBody);
                }
                ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
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
            Assert.Contains("10", result);
            Assert.DoesNotContain("microsoft.graph1", result);
        }

        [Fact]
        public async Task WritesADoubleAsync()
        {
            var sampleJson = @"
            {
                ""minimumAttendeePercentage"": 10
            }
            ";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            //THIS SNIPPET IS A PREVIEW FOR THE KIOTA BASED SDK. NON-PRODUCTION USE ONLY
            // Dependencies
            import ""@microsoft/msgraph-sdk-users"";
            import { FindMeetingTimesPostRequestBody } from ""@microsoft/msgraph-sdk-users/users/item/findMeetingTimes"";
            //other-imports

            const requestBody : FindMeetingTimesPostRequestBody = {
	            minimumAttendeePercentage : 10,
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.findMeetingTimes.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
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

            var expected = @"

            const requestBody = new ArrayBuffer(16);

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            async () => {
	            await graphServiceClient.applications.byApplicationId(""application-id"").logo.put(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
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

            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-chats"";
            import { ChatMessageHostedContent} from ""@microsoft/msgraph-sdk/models"";
            //other-imports

            const requestBody : ChatMessageHostedContent = {
	            contentBytes : ""wiubviuwbegviwubiu"",
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.chats.byChatId(""chat-id"").messages.byChatMessageId(""chatMessage-id"").hostedContents.post(requestBody);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesADatePayloadAsync()
        {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const requestBody : Message = {
	            receivedDateTime : new Date(""2021-08-30T20:00:00:00Z""),
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.messages.post(requestBody);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalDataAsync()
        {
            var samplePayload = @"
            {
              ""members@odata.bind"": [
                ""https://graph.microsoft.com/v1.0/directoryObjects/{id}"",
                ""https://graph.microsoft.com/v1.0/directoryObjects/{id}"",
                ""https://graph.microsoft.com/v1.0/directoryObjects/{id}""
                ]
            }
            ";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}")
            {
                Content = new StringContent(samplePayload, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const requestBody : Group = {
	            additionalData : {
		            ""members@odata.bind"" : [
			            ""https://graph.microsoft.com/v1.0/directoryObjects/{id}"",
			            ""https://graph.microsoft.com/v1.0/directoryObjects/{id}"",
			            ""https://graph.microsoft.com/v1.0/directoryObjects/{id}"",
		            ],
	            },
            };
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesAnArrayOfObjectsPayloadDataAsync()
        {
            const string userJsonObject = "{ \"body\": { \"contentType\": \"HTML\"}, \"extensions\": [{ \"dealValue\": 10000}]}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

                    const requestBody : Group = {
	                    extensions : [
		                    {
			                    additionalData : {
				                    dealValue : 10000,
			                    },
		                    },
	                    ],
	                    additionalData : {
		                    body : {
			                    contentType : ""HTML"",
		                    },
	                    },
                    };

                    // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

                    const result = async () => {
	                    await graphServiceClient.groups.byGroupId(""group-id"").patch(requestBody);
                    }
                    ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSelectQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const configuration = {
	            queryParameters : {
		            select: [""displayName"",""id""],
	            }
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.me.get(configuration);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesCountBooleanQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const configuration = {
	            queryParameters : {
		            count: true,
		            select: [""displayName"",""id""],
	            }
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.users.get(configuration);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSkipQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const configuration = {
	            queryParameters : {
		            skip: 10,
	            }
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.users.get(configuration);
            }
            ";
            
            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSelectExpandQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("select :", result);
        }

        [Fact]
        public async Task GeneratesRequestHeadersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"

            const configuration = {
	            headers : {
		            ""ConsistencyLevel"": ""eventual"",
	            }
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.groups.get(configuration);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GenerateAdditionalDataAsync()
        {
            var samplePayload = @"
            {
                ""createdDateTime"": ""2019-02-04T19:58:15.511Z"",
                ""from"": {
                    ""user"": {
                        ""id"": ""id-value"",
                        ""displayName"": ""Joh Doe"",
                        ""userIdentityType"": ""aadUser""
                    }
                },
                ""body"": {
                    ""contentType"": ""html"",
                    ""content"": ""Hello World""
                }
            }
            ";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/team-id/channels/19:4b6bed8d24574f6a9e436813cb2617d8@thread.tacv2/messages")
            {
                Content = new StringContent(samplePayload, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            // Dependencies
            import ""@microsoft/msgraph-sdk-teams"";
            import { ChatMessage, BodyTypeObject } from ""@microsoft/msgraph-sdk/models"";
            //other-imports

            const requestBody : ChatMessage = {
	            createdDateTime : new Date(""2019-02-04T19:58:15.511Z""),
	            from : {
		            user : {
			            id : ""id-value"",
			            displayName : ""Joh Doe"",
			            additionalData : {
				            ""userIdentityType"" : ""aadUser"",
			            },
		            },
	            },
	            body : {
		            contentType : BodyTypeObject.Html,
		            content : ""Hello World"",
	            },
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.teams.byTeamId(""team-id"").channels.byChannelId(""channel-id"").messages.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesEnumsWhenVariableIsEnumAsync()
        {
            const string payloadJson = @"{
                ""displayName"": ""Test create"",
                ""settings"": {
                    ""recurrence"": {
                        ""pattern"": {
                            ""type"": ""weekly"",
                            ""interval"": 1
                        },
                        ""range"": {
                            ""type"": ""noEnd"",
                            ""startDate"": ""2020-09-08T12:02:30.667Z""
                        }
                    }
                }
            }";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identityGovernance/accessReviews/definitions")
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            import ""@microsoft/msgraph-sdk-identitygovernance"";
            import { AccessReviewScheduleDefinition, RecurrencePatternTypeObject, RecurrenceRangeTypeObject } from ""@microsoft/msgraph-sdk/models"";
            import { DateOnly } from ""@microsoft/kiota-abstractions"";
            //other-imports

            const requestBody : AccessReviewScheduleDefinition = {
	            displayName : ""Test create"",
	            settings : {
		            recurrence : {
			            pattern : {
				            type : RecurrencePatternTypeObject.Weekly,
				            interval : 1,
			            },
			            range : {
				            type : RecurrenceRangeTypeObject.NoEnd,
				            startDate : DateOnly.parse(""2020-09-08T12:02:30.667Z""),
			            },
		            },
	            },
            };

            // To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=typescript

            const result = async () => {
	            await graphServiceClient.identityGovernance.accessReviews.definitions.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }
    }
}
