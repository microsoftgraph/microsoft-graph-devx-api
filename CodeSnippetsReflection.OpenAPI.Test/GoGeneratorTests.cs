﻿using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.OpenApi.Services;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class GoGeneratorTests {
        private const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
        private const string ServiceRootBetaUrl = "https://graph.microsoft.com/beta";
        private static OpenApiUrlTreeNode _v1TreeNode;
        private static OpenApiUrlTreeNode _betaTreeNode;
        private static async Task<OpenApiUrlTreeNode> GetV1TreeNode() {
            if(_v1TreeNode == null) {
                _v1TreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            }
            return _v1TreeNode;
        }
        private static async Task<OpenApiUrlTreeNode> GetBetaTreeNode() {
            if(_betaTreeNode == null) {
                _betaTreeNode = await SnippetModelTests.GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml");
            }
            return _betaTreeNode;
        }
        private readonly GoGenerator _generator = new();
        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPath() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(".Me().Messages()", result);
        }
        [Fact]
        public async Task GeneratesTheCorrectFluentAPIPathForIndexedCollections() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("messageId := \"message-id\"", result);
            Assert.Contains(".Me().MessagesById(&messageId)", result);
        }
        [Fact]
        public async Task GeneratesTheSnippetHeader() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient := msgraphsdk.NewGraphServiceClient(requestAdapter)", result);
        }
        [Fact]
        public async Task GeneratesTheGetMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get", result);
        }
        [Fact]
        public async Task GeneratesThePostMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Post", result);
        }
        [Fact]
        public async Task GeneratesThePatchMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Patch", result);
        }
        [Fact]
        public async Task GeneratesThePutMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Put", result);
        }
        [Fact]
        public async Task GeneratesTheDeleteMethodCall() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Delete", result);
            Assert.DoesNotContain("result, err :=", result);
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
            Assert.Contains("msgraphsdk.NewUser", result);
            Assert.Contains("SetAccountEnabled(&accountEnabled)", result);
            Assert.Contains("passwordProfile := msgraphsdk.NewPasswordProfile()", result);
            Assert.Contains("displayName := \"displayName-value\"", result);
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
            Assert.Contains("int64(10)", result);
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
            Assert.Contains("float64(10)", result);
        }
        [Fact]
        public async Task GeneratesABinaryPayload() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo") {
                Content = new ByteArrayContent(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10 })
            };
            requestPayload.Content.Headers.ContentType = new ("application/octet-stream");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("make([]byte, 0)", result);
        }
        [Fact]
        public async Task GeneratesABase64UrlPayload() {
            const string userJsonObject = "{\r\n  \"contentBytes\": \"wiubviuwbegviwubiu\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/chats/{{chat-id}}/messages/{{chatMessage-id}}/hostedContents") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("[]byte(", result);
        }
        [Fact]
        public async Task GeneratesADateTimeOffsetPayload() {
            const string userJsonObject = "{\r\n  \"receivedDateTime\": \"2021-08-30T20:00:00:00Z\"\r\n\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/me/messages") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains(", err := time.Parse(time.RFC3339,", result);
        }
        [Fact]
        public async Task GeneratesAnArrayPayloadInAdditionalData() {
            const string userJsonObject = "{\r\n  \"members@odata.bind\": [\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\",\r\n    \"https://graph.microsoft.com/v1.0/directoryObjects/{id}\"\r\n    ]\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/groups/{{group-id}}") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("map[string]interface{}{", result);
            Assert.Contains("[]String {", result);
            Assert.Contains("SetAdditionalData", result);
            Assert.Contains("members", result); // property name hasn't been changed
        }
        [Fact]
        public async Task GeneratesSelectQueryParameters() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/me?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1TreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Select: \"displayName", result);
            Assert.Contains("Q: ", result);
            Assert.Contains("MeRequestBuilderGetQueryParameters", result);
            Assert.Contains("MeRequestBuilderGetOptions", result);
            Assert.Contains("options :=", result);
            Assert.Contains("requestParameters :=", result);
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
            Assert.Contains("\"ConsistencyLevel\": \"eventual\"", result);
            Assert.Contains("H: headers", result);
        }
        [Fact]
        public async Task SupportsODataOldIndexFormat() {
            const string userJsonObject = "{\r\n\"@odata.type\": \"#microsoft.graph.fileAttachment\",\r\n\"name\": \"menu.txt\",\r\n\"contentBytes\": \"bWFjIGFuZCBjaGVlc2UgdG9kYXk=\"\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/me/events('AAMkAGI1AAAt9AHjAAA=')/attachments") {
                Content = new StringContent(userJsonObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Me().EventsById(&eventId).Attachments().Post(options)", result);
        }
        [Fact]
        public async Task ParsesThePayloadEvenIfContentTypeIsMissing() {
            const string messageObject = "{\r\n\"createdDateTime\":\"2019-02-04T19:58:15.511Z\",\r\n\"from\":{\r\n\"user\":{\r\n\"id\":\"id-value\",\r\n\"displayName\":\"Joh Doe\",\r\n\"userIdentityType\":\"aadUser\"\r\n}\r\n},\r\n\"body\":{\r\n\"contentType\":\"html\",\r\n\"content\":\"Hello World\"\r\n}\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/teams/57fb72d0-d811-46f4-8947-305e6072eaa5/channels/19:4b6bed8d24574f6a9e436813cb2617d8@thread.tacv2/messages") {
                Content = new StringContent(messageObject, Encoding.UTF8)
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.TeamsById(&teamId).ChannelsById(&channelId).Messages().Post(options)", result);
        }
        [Fact]
        public async Task WritesEmptyPrimitiveArrays() {
            const string messageObject = "{\r\n\"displayName\": \"Demo app for documentation\",\r\n\"state\": \"disabled\",\r\n\"conditions\": {\r\n\"signInRiskLevels\": [\r\n\"high\",\r\n\"medium\"\r\n],\r\n\"clientAppTypes\": [\r\n\"mobileAppsAndDesktopClients\",\r\n\"exchangeActiveSync\",\r\n\"other\"\r\n],\r\n\"applications\": {\r\n\"includeApplications\": [\r\n\"All\"\r\n],\r\n\"excludeApplications\": [\r\n\"499b84ac-1321-427f-aa17-267ca6975798\",\r\n\"00000007-0000-0000-c000-000000000000\",\r\n\"de8bc8b5-d9f9-48b1-a8ad-b748da725064\",\r\n\"00000012-0000-0000-c000-000000000000\",\r\n\"797f4846-ba00-4fd7-ba43-dac1f8f63013\",\r\n\"05a65629-4c1b-48c1-a78b-804c4abdd4af\",\r\n\"7df0a125-d3be-4c96-aa54-591f83ff541c\"\r\n],\r\n\"includeUserActions\": []\r\n},\r\n\"users\": {\r\n\"includeUsers\": [\r\n\"a702a13d-a437-4a07-8a7e-8c052de62dfd\"\r\n],\r\n\"excludeUsers\": [\r\n\"124c5b6a-ffa5-483a-9b88-04c3fce5574a\",\r\n\"GuestsOrExternalUsers\"\r\n],\r\n\"includeGroups\": [],\r\n\"excludeGroups\": [],\r\n\"includeRoles\": [\r\n\"9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3\",\r\n\"cf1c38e5-3621-4004-a7cb-879624dced7c\",\r\n\"c4e39bd9-1100-46d3-8c65-fb160da0071f\"\r\n],\r\n\"excludeRoles\": [\r\n\"b0f54661-2d74-4c50-afa3-1ec803f12efe\"\r\n]\r\n},\r\n\"platforms\": {\r\n\"includePlatforms\": [\r\n\"all\"\r\n],\r\n\"excludePlatforms\": [\r\n\"iOS\",\r\n\"windowsPhone\"\r\n]\r\n},\r\n\"locations\": {\r\n\"includeLocations\": [\r\n\"AllTrusted\"\r\n],\r\n\"excludeLocations\": [\r\n\"00000000-0000-0000-0000-000000000000\",\r\n\"d2136c9c-b049-47ae-b9cf-316e04ef7198\"\r\n]\r\n},\r\n\"deviceStates\": {\r\n\"includeStates\": [\r\n\"All\"\r\n],\r\n\"excludeStates\": [\r\n\"Compliant\"\r\n]\r\n}\r\n},\r\n\"grantControls\": {\r\n\"operator\": \"OR\",\r\n\"builtInControls\": [\r\n\"mfa\",\r\n\"compliantDevice\",\r\n\"domainJoinedDevice\",\r\n\"approvedApplication\",\r\n\"compliantApplication\"\r\n],\r\n\"customAuthenticationFactors\": [],\r\n\"termsOfUse\": [\r\n\"ce580154-086a-40fd-91df-8a60abac81a0\",\r\n\"7f29d675-caff-43e1-8a53-1b8516ed2075\"\r\n]\r\n},\r\n\"sessionControls\": {\r\n\"applicationEnforcedRestrictions\": null,\r\n\"persistentBrowser\": null,\r\n\"cloudAppSecurity\": {\r\n\"cloudAppSecurityType\": \"blockDownloads\",\r\n\"isEnabled\": true\r\n},\r\n\"signInFrequency\": {\r\n\"value\": 4,\r\n\"type\": \"hours\",\r\n\"isEnabled\": true\r\n}\r\n}\r\n}";
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootBetaUrl}/identity/conditionalAccess/policies") {
                Content = new StringContent(messageObject, Encoding.UTF8, "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("SetIncludeUserActions", result);
        }
        [Fact]
        public async Task DoesntReplaceODataFunctionCalls() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/devices/{{objectId}}/usageRights?$filter=state in ('active', 'suspended') and serviceIdentifier in ('ABCD')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("state%20in%20('active',%20'suspended')%20and%20serviceIdentifier%20in%20('ABCD')", result);
        }
        [Fact]
        public async Task FindsPathItemsWithDifferentCasing() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/directory/deleteditems/microsoft.graph.group");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Directory().DeletedItemsById(&directoryObjectId).Get(nil)", result);
        }
        [Fact]
        public async Task DoesntFailOnTerminalSlash() {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/me/messages/AAMkADYAAAImV_jAAA=/?$expand=microsoft.graph.eventMessage/event");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaTreeNode());
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("graphClient.Me().MessagesById(&messageId).Get(options)", result);
        }
        //TODO test for DateTimeOffset
    }
}
