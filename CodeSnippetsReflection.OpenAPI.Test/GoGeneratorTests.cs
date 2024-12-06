using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class GoGeneratorTests : OpenApiSnippetGeneratorTestBase
    {
        private readonly GoGenerator _generator = new();

        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me().Messages()", result);
        }
        [Fact]
        public async Task GeneratesMeImportFromUserPackageAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages?$select=sender,subject");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("import", result);
            Assert.Contains("graphusers \"github.com/microsoftgraph/msgraph-sdk-go/users\"", result);
            Assert.Contains("msgraphsdk \"github.com/microsoftgraph/msgraph-sdk-go\"", result);
            Assert.Contains(".Me().Messages()", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathForIndexedCollectionsAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me().Messages().ByMessageId(\"message-id\")", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathForIndexedCollectionsWithMultipleParamsAsync() {
            var sampleJson = @"
            {
                ""comment"": ""Updating the latest guidelines""
            }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/drives/{{drive-id}}/items/{{item-id}}/checkin"){
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Drives().ByDriveId(\"drive-id\").Items().ByDriveItemId(\"driveItem-id\").Checkin().Post(context.Background(), requestBody, nil)", result);
        }
        [Fact]
        public async Task IgnoreOdataTypeWhenGeneratingAsync() {
            var sampleJson = @"
                {
                ""@odata.type"": ""microsoft.graph.socialIdentityProvider"",
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
            Assert.DoesNotContain("@odata.type", result);
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
            Assert.Contains("requestBody := graphusers.NewItemAssignLicensePostRequestBody()", result);
            Assert.Contains("disabledPlans := []uuid.UUID {", result);
            Assert.Contains("removeLicenses := []uuid.UUID {", result);
            Assert.Contains("uuid.MustParse(\"bea13e0c-3828-4daa-a392-28af7ff61a0f\"),", result);
        }
        [Fact]
        public async Task GeneratesTheSnippetHeaderAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("To initialize your graphClient, see https://learn.microsoft.com/en-us/graph/sdks/create-client?from=snippets&tabs=go", result);
        }
        [Fact]
        public async Task GeneratesMultipleImportStatementsAsync()
        {
            var bodyContent = @"
            {
                ""customKeyIdentifier"": null,
                ""endDateTime"": ""2021-09-09T19:50:29.3086381Z"",
                ""keyId"": ""f0b0b335-1d71-4883-8f98-567911bfdca6"",
                ""startDateTime"": ""2019-09-09T19:50:29.3086381Z"",
                ""secretText"": ""[6gyXA5S20@MN+WRXAJ]I-TO7g1:h2P8"",
                ""hint"": ""[6g"",
                ""displayName"": ""Password friendly name""
            }
            ";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{application-id}}/addPassword")
                {
                    Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphapplications \"github.com/microsoftgraph/msgraph-sdk-go/applications\"", result);
        }
        [Fact]
        public async Task AllowsNestedModelsNameSpaceAsync()
        {
            var bodyContent = @"
            {
              ""labels"": [
                        {
                            ""languageTag"" : ""en-US"",
                            ""name"" : ""Car"",
                            ""isDefault"" : true
                        }
              ]
            }
            ";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/sites/microsoft.sharepoint.com,b9b0bc03-cbc4-40d2-aba9-2c9dd9821ddf,6a742cee-9216-4db5-8046-13a595684e74/termStore/sets/{{set-id}}/children")
                {
                    Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphmodelstermstore \"github.com/microsoftgraph/msgraph-sdk-go/models/termstore\"", result);
            Assert.Contains("requestBody := graphmodelstermstore.NewTerm()", result);
        }
        [Fact]
        public async Task GeneratesTheGetMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("messages, err := graphClient.Me().Messages().Get(context.Background(), nil)", result);
        }
        [Fact]
        public async Task GeneratesThePostMethodCallAsync() {
            const string messageObjectJson = "{\"subject\": \"Test Subject\"}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages") {
                Content = new StringContent(messageObjectJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Post", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("messages, err := graphClient.Me().Messages().Post(context.Background(), requestBody, nil)", result);
        }
        [Fact]
        public async Task GeneratesThePatchMethodCallAsync() {
            const string messageObjectJson = "{\"subject\": \"Test Subject\"}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}") {
                Content = new StringContent(messageObjectJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Patch", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("graphClient.Me().Messages().ByMessageId(\"message-id\").Patch(context.Background(), requestBody, nil)", result);
        }
        [Fact]
        public async Task GeneratesThePutMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
                Content = new StreamContent(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })) {
                    Headers = {
                        ContentType = new MediaTypeHeaderValue("application/octet-stream")
                    }
                }
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Put", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("graphClient.Applications().ByApplicationId(\"application-id\").Logo().PutAsLogoPutResponse(context.Background(), requestBody, nil)", result);
        }

        [Theory]
        [InlineData("/appCatalogs/teamsApps?requiresReview=true","graphClient.AppCatalogs().TeamsApps().Post(", "POST")]
        [InlineData("/me/photos/48x48/$value","Value().Get(")]
        [InlineData("/users/some-id/mailFolders/delta","GetAsDeltaGetResponse")]
        [InlineData("/identityGovernance/accessReviews/definitions/filterByCurrentUser(on='reviewer')", "GetAsFilterByCurrentUserWithOnGetResponse")]
        public async Task GeneratesTheInlineSchemaFunctionPrefixCorrectlyAsync(string inputPath, string expectedSuffix, string method = "")
        {
            using var requestPayload = new HttpRequestMessage(string.IsNullOrEmpty(method) ? HttpMethod.Get : new HttpMethod(method), $"{ServiceRootUrl}{inputPath}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(expectedSuffix, result);
        }
        [Fact]
        public async Task GeneratesTheDeleteMethodCallAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Delete", result);
            Assert.DoesNotContain("result, err :=", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("graphClient.Me().Messages().ByMessageId(\"message-id\").Delete(context.Background(), nil)", result);
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
            Assert.Contains("graphmodels.NewUser", result);
            Assert.Contains("SetAccountEnabled(&accountEnabled)", result);
            Assert.Contains("passwordProfile := graphmodels.NewPasswordProfile()", result);
            Assert.Contains("displayName := \"displayName-value\"", result);
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
            Assert.Contains("int64(10)", result);
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
            Assert.Contains("float64(10)", result);
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
            Assert.Contains("make([]byte, 0)", result);
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
            Assert.Contains("[]byte(", result);
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
            Assert.Contains(", err := time.Parse(time.RFC3339,", result);
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
            Assert.Contains("map[string]interface{}{", result);
            Assert.Contains("[]string {", result);
            Assert.Contains("SetAdditionalData", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
        }
        [Fact]
        public async Task GeneratesSelectQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Select: [] string {\"displayName\"", result);
            Assert.Contains("QueryParameters: ", result);
            Assert.Contains("MeRequestBuilderGetQueryParameters", result);
            Assert.Contains("MeRequestBuilderGetRequestConfiguration", result);
            Assert.Contains("configuration :=", result);
            Assert.Contains("requestParameters :=", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("me, err := graphClient.Me().Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task GeneratesNestedParameterNamesAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{id}}?$select=displayName,givenName,postalCode,identities");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Select: [] string {\"displayName\",\"givenName\",\"postalCode\",\"identities\"}", result);
            Assert.Contains("QueryParameters: ", result);
            Assert.Contains("&graphusers.UserItemRequestBuilderGetQueryParameters", result);
            Assert.Contains("&graphusers.UserItemRequestBuilderGetRequestConfiguration", result);
            Assert.Contains("configuration :=", result);
            Assert.Contains("requestParameters :=", result);
            Assert.Contains("users, err := graphClient.Users().ByUserId(\"user-id\").Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task GeneratesODataTypesAreEscapedAsync()
        {
            const string jsonObject = @"
{
  ""@odata.id"": ""https://graph.microsoft.com/v1.0/directoryObjects/{id}""
}
";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups/{{group-id}}/members/$ref")
            {
                Content = new StringContent(jsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestBody := graphmodels.NewReferenceCreate()", result);
            Assert.Contains("requestBody.SetOdataId(&odataId)", result);
            Assert.Contains("odataId := \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"", result);
            Assert.Contains("graphClient.Groups().ByGroupId(\"group-id\").Members().Ref().Post(context.Background(), requestBody, nil)", result);
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
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("users, err := graphClient.Users().Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task GeneratesSkipQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("\"10\"", result);
            Assert.Contains("10", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("users, err := graphClient.Users().Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task GeneratesSelectExpandQueryParametersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=id,displayName),teams($select=id,displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Expand", result);
            Assert.Contains("members($select=id,displayName)", result);
            Assert.DoesNotContain("Select", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("groups, err := graphClient.Groups().Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task GeneratesRequestHeadersAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("import", result);
            Assert.Contains("abstractions \"github.com/microsoft/kiota-abstractions-go\"", result);
            Assert.Contains("headers := abstractions.NewRequestHeaders()", result);
            Assert.Contains("headers.Add(\"ConsistencyLevel\", \"eventual\")", result);
            Assert.Contains("graphgroups.GroupsRequestBuilderGetRequestConfiguration", result);
            Assert.Contains("Headers: headers", result);
            Assert.DoesNotContain("WithRequestConfigurationAndResponseHandler", result);
            Assert.Contains("groups, err := graphClient.Groups().Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task SupportsODataOldIndexFormatAsync() {
            const string userJsonObject = "{\r\n\"@odata.type\": \"#microsoft.graph.fileAttachment\",\r\n\"name\": \"menu.txt\",\r\n\"contentBytes\": \"bWFjIGFuZCBjaGVlc2UgdG9kYXk=\"\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/me/events('AAMkAGI1AAAt9AHjAAA=')/attachments") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Me().Events().ByEventId(\"event-id\").Attachments().Post(context.Background(), requestBody, nil)", result);
        }
        [Fact]
        public async Task ParsesThePayloadEvenIfContentTypeIsMissingAsync() {
            const string messageObject = "{\r\n\"createdDateTime\":\"2019-02-04T19:58:15.511Z\",\r\n\"from\":{\r\n\"user\":{\r\n\"id\":\"id-value\",\r\n\"displayName\":\"Joh Doe\",\r\n\"userIdentityType\":\"aadUser\"\r\n}\r\n},\r\n\"body\":{\r\n\"contentType\":\"html\",\r\n\"content\":\"Hello World\"\r\n}\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/57fb72d0-d811-46f4-8947-305e6072eaa5/channels/19:4b6bed8d24574f6a9e436813cb2617d8@thread.tacv2/messages") {
                Content = new StringContent(messageObject, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Teams().ByTeamId(\"team-id\").Channels().ByChannelId(\"channel-id\").Messages().PostAsMessagesPostResponse(context.Background(), requestBody, nil)", result);
        }
        [Fact]
        public async Task WritesEmptyPrimitiveArraysAsync() {
            const string messageObject = "{\r\n\"displayName\": \"Demo app for documentation\",\r\n\"state\": \"disabled\",\r\n\"conditions\": {\r\n\"signInRiskLevels\": [\r\n\"high\",\r\n\"medium\"\r\n],\r\n\"clientAppTypes\": [\r\n\"mobileAppsAndDesktopClients\",\r\n\"exchangeActiveSync\",\r\n\"other\"\r\n],\r\n\"applications\": {\r\n\"includeApplications\": [\r\n\"All\"\r\n],\r\n\"excludeApplications\": [\r\n\"499b84ac-1321-427f-aa17-267ca6975798\",\r\n\"00000007-0000-0000-c000-000000000000\",\r\n\"de8bc8b5-d9f9-48b1-a8ad-b748da725064\",\r\n\"00000012-0000-0000-c000-000000000000\",\r\n\"797f4846-ba00-4fd7-ba43-dac1f8f63013\",\r\n\"05a65629-4c1b-48c1-a78b-804c4abdd4af\",\r\n\"7df0a125-d3be-4c96-aa54-591f83ff541c\"\r\n],\r\n\"includeUserActions\": []\r\n},\r\n\"users\": {\r\n\"includeUsers\": [\r\n\"a702a13d-a437-4a07-8a7e-8c052de62dfd\"\r\n],\r\n\"excludeUsers\": [\r\n\"124c5b6a-ffa5-483a-9b88-04c3fce5574a\",\r\n\"GuestsOrExternalUsers\"\r\n],\r\n\"includeGroups\": [],\r\n\"excludeGroups\": [],\r\n\"includeRoles\": [\r\n\"9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3\",\r\n\"cf1c38e5-3621-4004-a7cb-879624dced7c\",\r\n\"c4e39bd9-1100-46d3-8c65-fb160da0071f\"\r\n],\r\n\"excludeRoles\": [\r\n\"b0f54661-2d74-4c50-afa3-1ec803f12efe\"\r\n]\r\n},\r\n\"platforms\": {\r\n\"includePlatforms\": [\r\n\"all\"\r\n],\r\n\"excludePlatforms\": [\r\n\"iOS\",\r\n\"windowsPhone\"\r\n]\r\n},\r\n\"locations\": {\r\n\"includeLocations\": [\r\n\"AllTrusted\"\r\n],\r\n\"excludeLocations\": [\r\n\"00000000-0000-0000-0000-000000000000\",\r\n\"d2136c9c-b049-47ae-b9cf-316e04ef7198\"\r\n]\r\n},\r\n\"deviceStates\": {\r\n\"includeStates\": [\r\n\"All\"\r\n],\r\n\"excludeStates\": [\r\n\"Compliant\"\r\n]\r\n}\r\n},\r\n\"grantControls\": {\r\n\"operator\": \"OR\",\r\n\"builtInControls\": [\r\n\"mfa\",\r\n\"compliantDevice\",\r\n\"domainJoinedDevice\",\r\n\"approvedApplication\",\r\n\"compliantApplication\"\r\n],\r\n\"customAuthenticationFactors\": [],\r\n\"termsOfUse\": [\r\n\"ce580154-086a-40fd-91df-8a60abac81a0\",\r\n\"7f29d675-caff-43e1-8a53-1b8516ed2075\"\r\n]\r\n},\r\n\"sessionControls\": {\r\n\"applicationEnforcedRestrictions\": null,\r\n\"persistentBrowser\": null,\r\n\"cloudAppSecurity\": {\r\n\"cloudAppSecurityType\": \"blockDownloads\",\r\n\"isEnabled\": true\r\n},\r\n\"signInFrequency\": {\r\n\"value\": 4,\r\n\"type\": \"hours\",\r\n\"isEnabled\": true\r\n}\r\n}\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/identity/conditionalAccess/policies") {
                Content = new StringContent(messageObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("SetIncludeUserActions", result);
        }
        [Fact]
        public async Task WriteCorrectFunctionNameWithParametersAsModelNameAsync()
        {
            const string messageObject = "{\r\n\"displayName\": \"Display name\"\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/applications(uniqueName='app-65278')")
            {
                Content = new StringContent(messageObject, Encoding.UTF8, "application/json")
            };
            requestPayload.Headers.Add("Prefer", "create-if-missing");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphapplicationswithuniquename \"github.com/microsoftgraph/msgraph-sdk-go/applicationswithuniquename\"", result);
            Assert.Contains("graphapplicationswithuniquename.ApplicationsWithUniqueNameRequestBuilderPatchRequestConfiguration", result);
        }
        [Fact]
        public async Task WriteCorrectTypesForFilterParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get,
                $"{ServiceRootBetaUrl}/identityGovernance/accessReviews/definitions?$top=100&$skip=0");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestTop := int32(100)", result);
            Assert.Contains("requestSkip := int32(0)", result);
        }
        [Fact]
        public async Task DoesNotNormalizeKeysForMapsAsync()
        {
            var sampleJson = @"
            {
               ""template@odata.bind"":""https://graph.microsoft.com/v1.0/teamsTemplates('standard')"",
               ""displayName"":""My Sample Team"",
               ""description"":""My Sample Team’s Description"",
               ""members"":[
                  {
                     ""@odata.type"":""#microsoft.graph.aadUserConversationMember"",
                     ""roles"":[
                        ""owner""
                     ],
                     ""user@odata.bind"":""https://graph.microsoft.com/v1.0/users('0040b377-61d8-43db-94f5-81374122dc7e')""
                  }
               ]
            }
            ";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams")
            {
                Content = new StringContent(sampleJson, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("\"user@odata.bind\" : \"https://graph.microsoft.com/v1.0/users('0040b377-61d8-43db-94f5-81374122dc7e')\"", result);
            Assert.Contains("\"template@odata.bind\" : \"https://graph.microsoft.com/v1.0/teamsTemplates('standard')\"", result);
        }

        /**

        //TODO Diagnose exception : System.ArgumentException : An item with the same key has already been added. Key: 20
        [Fact]
        public async Task DoesntReplaceODataFunctionCalls() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/devices/{{objectId}}/usageRights?$filter=state in ('active', 'suspended') and serviceIdentifier in ('ABCD')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadata());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("state%20in%20('active',%20'suspended')%20and%20serviceIdentifier%20in%20('ABCD')", result);
            Assert.Contains("WithRequestConfigurationAndResponseHandler", result);
        }
        **/

        [Fact]
        public async Task FindsPathItemsWithDifferentCasingAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Directory().DeletedItems().GraphGroup().Get(context.Background(), nil)", result);
        }
        [Fact]
        public async Task DoesntFailOnTerminalSlashAsync() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Me().Messages().ByMessageId(\"message-id\").Get(context.Background(), configuration)", result);
        }
        [Fact]
        public async Task IncludesRequestBodyClassNameAsync() {
            const string payloadBody = "{\r\n  \"passwordCredential\": {\r\n    \"displayName\": \"Password friendly name\"\r\n  }\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/applications/{{id}}/addPassword") {
                Content = new StringContent(payloadBody, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("NewAddPasswordPostRequestBody", result);
        }

        [Fact]
        public async Task GeneratePathParametersInFluentPathAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/reports/getYammerActivityCounts(period='D7')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("period := \"{period}\"", result);
            Assert.Contains("graphClient.Reports().GetYammerActivityCountsWithPeriod(&period).Get(context.Background(), nil)", result);
        }

        [Fact]
        public async Task GeneratePathParametersInFluentPathWithPeriodsInNameAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/communications/callRecords/getPstnCalls(fromDateTime=2019-11-01,toDateTime=2019-12-01)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("fromDateTime , err := time.Parse(time.RFC3339, \"{fromDateTime}\")", result);
            Assert.Contains("toDateTime , err := time.Parse(time.RFC3339, \"{toDateTime}\")", result);
            Assert.Contains("microsoftGraphCallRecordsGetPstnCalls, err := graphClient.Communications().CallRecords().MicrosoftGraphCallRecordsGetPstnCallsWithFromDateTimeWithToDateTime(&fromDateTime, &toDateTime).GetAsGetPstnCallsWithFromDateTimeWithToDateTimeGetResponse(context.Background(), nil)", result);
        }
        
        [Fact]
        public async Task GeneratesConfigurationObject()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/{{id}}/mailFolders('inbox')/messages/delta?changeType=created&$select=subject,from,isRead,body,receivedDateTime");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("requestParameters := &graphusers.ItemMailFoldersItemMessagesDeltaRequestBuilderGetQueryParameters{", result);
            Assert.Contains("configuration := &graphusers.ItemMailFoldersItemMessagesDeltaRequestBuilderGetRequestConfiguration{", result);
            Assert.Contains("delta, err := graphClient.Users().ByUserId(\"user-id\").MailFolders().ByMailFolderId(\"mailFolder-id\").Messages().Delta().GetAsDeltaGetResponse(context.Background(), configuration)", result);
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
                    ""minimumAttendeePercentage"": 200
            }";

            using var requestPayload =
                new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/findMeetingTimes")
                {
                    Content = new StringContent(bodyContent, Encoding.UTF8, "application/json")
                };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("maxCandidates := int32(100)", result);
            Assert.Contains("minimumAttendeePercentage := float64(200)", result);
            Assert.Contains("isOrganizerOptional := false", result);
        }
        [Fact]
        public async Task GeneratesPathSegmentsUsingVariableNameAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/drive/items/{{id}}/workbook/worksheets/{{id|name}}/charts");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Drives().ByDriveId(\"drive-id\").Items().ByDriveItemId(\"driveItem-id\").Workbook().Worksheets().ByWorkbookWorksheetId(\"workbookWorksheet-id\").Charts().Get(context.Background(), nil)", result);
        }
        //TODO test for DateTimeOffset
    }
}
