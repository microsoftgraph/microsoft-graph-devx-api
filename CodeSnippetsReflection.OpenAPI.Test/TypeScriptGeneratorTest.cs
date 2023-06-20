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
        public async Task GeneratesTheCorrectFluentAPIPath()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});


            const result = async () => {
	            await graphServiceClient.me.messages.get();
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesClassWithDefaultBodyWhenSchemaNotPresent()
        {
            const string userJsonObject = "{  \"decision\": \"Approve\",  \"justification\": \"All principals with access need continued access to the resource (Marketing Group) as all the principals are on the marketing team\",  \"resourceId\": \"a5c51e59-3fcd-4a37-87a1-835c0c21488a\"}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/identityGovernance/accessReviews/definitions/e6cafba0-cbf0-4748-8868-0810c7f4cc06/instances/1234fba0-cbf0-6778-8868-9999c7f4cc06/batchRecordDecisions")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody : BatchRecordDecisionsPostRequestBody = {
	            decision : ""Approve"",
	            justification : ""All principals with access need continued access to the resource (Marketing Group) as all the principals are on the marketing team"",
	            resourceId : ""a5c51e59-3fcd-4a37-87a1-835c0c21488a"",
            };

            const result = async () => {
	            await graphServiceClient.identityGovernance.accessReviews.definitionsById(""accessReviewScheduleDefinition-id"").instancesById(""accessReviewInstance-id"").batchRecordDecisions.post(requestBody);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathForIndexedCollections()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});


            const result = async () => {
	            await graphServiceClient.me.messagesById(""message-id"").get();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesTheSnippetInitializationStatement()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("const graphServiceClient = GraphServiceClient.init({authProvider});", result);
        }

        [Fact]
        public async Task GeneratesTheGetMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});


            const result = async () => {
	            await graphServiceClient.me.messages.get();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePostMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
                const result = async () => {
	            await graphServiceClient.me.messages.post();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePatchMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
                const result = async () => {
	            await graphServiceClient.me.messagesById(""message-id"").patch();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesThePutMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("put", result);
        }

        [Fact]
        public async Task GeneratesTheDeleteMethodCall()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});


            const result = async () => {
	            await graphServiceClient.me.messagesById(""message-id"").delete();
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task WritesTheRequestPayload()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
                const graphServiceClient = GraphServiceClient.init({authProvider});

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

                const result = async () => {
	                await graphServiceClient.users.post(requestBody);
                }
                ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task WritesALongAndFindsAnAction()
        {
            const string userJsonObject = "{\r\n  \"chainId\": 10\r\n\r\n}";

            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/{{team-id}}/sendActivityNotification")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("10", result);
            Assert.DoesNotContain("microsoft.graph1", result);
        }

        [Fact]
        public async Task WritesADouble()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody : FindMeetingTimesPostRequestBody = {
	            minimumAttendeePercentage : 10,
            };

            const result = async () => {
	            await graphServiceClient.me.findMeetingTimes.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesABinaryPayload()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo")
            {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            requestPayload.Content.Headers.ContentType = new("application/octet-stream");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody = new ArrayBuffer(16);

            async () => {
	            await graphServiceClient.applicationsById(""application-id"").logo.put(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesABase64UrlPayload()
        {
            const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody : ChatMessageHostedContent = {
	            contentBytes : ""wiubviuwbegviwubiu"",
            };

            const result = async () => {
	            await graphServiceClient.chatsById(""chat-id"").messagesById(""chatMessage-id"").hostedContents.post(requestBody);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesADatePayload()
        {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody : Message = {
	            receivedDateTime : new Date(""2021-08-30T20:00:00:00Z""),
            };

            const result = async () => {
	            await graphServiceClient.me.messages.post(requestBody);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalData()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

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
        public async Task GeneratesAnArrayOfObjectsPayloadData()
        {
            const string userJsonObject = "{ \"body\": { \"contentType\": \"HTML\"}, \"extensions\": [{ \"dealValue\": 10000}]}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}")
            {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
                    const graphServiceClient = GraphServiceClient.init({authProvider});

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

                    const result = async () => {
	                    await graphServiceClient.groupsById(""group-id"").patch(requestBody);
                    }
                    ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSelectQueryParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const configuration = {
	            queryParameters : {
		            select: [""displayName"",""id""],
	            }
            };

            const result = async () => {
	            await graphServiceClient.me.get(configuration);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesCountBooleanQueryParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true&$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const configuration = {
	            queryParameters : {
		            count: true,
		            select: [""displayName"",""id""],
	            }
            };

            const result = async () => {
	            await graphServiceClient.users.get(configuration);
            }
            ";


            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSkipQueryParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const configuration = {
	            queryParameters : {
		            skip: 10,
	            }
            };

            const result = async () => {
	            await graphServiceClient.users.get(configuration);
            }
            ";
            
            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesSelectExpandQueryParameters()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("select :", result);
        }

        [Fact]
        public async Task GeneratesRequestHeaders()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const configuration = {
	            headers : {
		            ""ConsistencyLevel"": ""eventual"",
	            }
            };

            const result = async () => {
	            await graphServiceClient.groups.get(configuration);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GenerateAdditionalData()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

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
		            contentType : BodyType.Html,
		            content : ""Hello World"",
	            },
            };

            const result = async () => {
	            await graphServiceClient.teamsById(""team-id"").channelsById(""channel-id"").messages.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }

        [Fact]
        public async Task GeneratesEnumsWhenVariableIsEnum()
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
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadata());
            var result = _generator.GenerateCodeSnippet(snippetModel);

            var expected = @"
            const graphServiceClient = GraphServiceClient.init({authProvider});

            const requestBody : AccessReviewScheduleDefinition = {
	            displayName : ""Test create"",
	            settings : {
		            recurrence : {
			            pattern : {
				            type : RecurrencePatternType.Weekly,
				            interval : 1,
			            },
			            range : {
				            type : RecurrenceRangeType.NoEnd,
				            startDate : ""2020-09-08T12:02:30.667Z"",
			            },
		            },
	            },
            };

            const result = async () => {
	            await graphServiceClient.identityGovernance.accessReviews.definitions.post(requestBody);
            }
            ";

            AssertExtensions.ContainsIgnoreWhiteSpace(expected, result);
        }
    }
}
