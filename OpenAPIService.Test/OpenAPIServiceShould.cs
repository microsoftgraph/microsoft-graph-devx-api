// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;
using OpenAPIService.Interfaces;
using UtilityService;
using Xunit;

namespace OpenAPIService.Test
{
    public class OpenApiServiceShould
    {
        private const string Title = "Partial Graph API";
        private const string GraphVersion = "mock";
        private readonly OpenApiDocument _graphMockSource = null;
        private readonly IOpenApiService _openApiService;
        private readonly OpenApiDocumentCreatorMock _openAPIDocumentCreatorMock;
        private static string ConfigFilePath => Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json");

        public OpenApiServiceShould()
        {
            _openApiService = OpenApiDocumentCreatorMock.GetOpenApiService(ConfigFilePath);
            _openAPIDocumentCreatorMock = new OpenApiDocumentCreatorMock(_openApiService);

            // Create OpenAPI document with default OpenApiStyle = Plain
            _graphMockSource = _openAPIDocumentCreatorMock.GetGraphOpenApiDocument("Mock", false);
        }

        [Fact]
        public void FormatPathFunctionsOfStringDataTypesWithSingleQuotationMarks()
        {
            // Arrange
            var operationId_1 = "reports.getTeamsUserActivityCounts";
            var operationId_2 = "reports.getTeamsUserActivityUserDetail-a3f1";

            var predicate_1 = _openApiService.CreatePredicate(operationIds: operationId_1,
                                                             tags: null,
                                                             url: null,
                                                             source: _graphMockSource);

            var predicate_2 = _openApiService.CreatePredicate(operationIds: operationId_2,
                                                             tags: null,
                                                             url: null,
                                                             source: _graphMockSource);

            // Act
            var subsetOpenApiDocument_1 = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate_1);
            var subsetOpenApiDocument_2 = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate_2);

            // Assert
            Assert.Collection(subsetOpenApiDocument_1.Paths,
               item =>
               {
                   Assert.Equal("/reports/microsoft.graph.getTeamsUserActivityCounts(period='{period}')", item.Key);
               });
            Assert.Collection(subsetOpenApiDocument_2.Paths,
               item =>
               {
                   Assert.Equal("/reports/microsoft.graph.getTeamsUserActivityUserDetail(date={date})", item.Key);
               });
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData("users.user.ListUser", "users.user", "/users")]
        [InlineData("users.user.ListUser", "users.user", null)]
        [InlineData("users.user.ListUser", null, "/users")]
        [InlineData(null, "users.user", "/users")]
        public void ThrowsInvalidOperationExceptionInCreatePredicateWhenInvalidNumberOfArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Act and Assert
            if (string.IsNullOrEmpty(operationIds) &&
                string.IsNullOrEmpty(tags) &&
                string.IsNullOrEmpty(url))
            {
                var message = Assert.Throws<InvalidOperationException>(() =>
                    _openApiService.CreatePredicate(operationIds: operationIds,
                                                   tags: tags,
                                                   url: url,
                                                   source: _graphMockSource)).Message;
                Assert.Equal("Either operationIds, tags or url need to be specified.", message);
            }
            else
            {
                var message = Assert.Throws<InvalidOperationException>(() =>
                    _openApiService.CreatePredicate(operationIds: operationIds,
                                                   tags: tags,
                                                   url: url,
                                                   source: _graphMockSource)).Message;

                if (url != null && (operationIds != null || tags != null))
                {
                    Assert.Equal("Cannot filter by url and either operationIds and tags at the same time.", message);
                }
                else if (operationIds != null && tags != null)
                {
                    Assert.Equal("Cannot filter by operationIds and tags at the same time.", message);
                }
            }
        }

        [Fact]
        public void ThrowsArgumentExceptionInCreatePredicateWhenNonExistentUrlArgumentIsSpecified()
        {
            // Act and Assert
            var message = Assert.Throws<ArgumentException>(() => _openApiService.CreatePredicate(operationIds: null,
                                                                                                tags: null,
                                                                                                url: "/foo",
                                                                                                source: _graphMockSource,
                                                                                                graphVersion: GraphVersion)).Message;
            Assert.Equal("The url supplied could not be found.", message);
        }

        [Fact]
        public void ThrowsArgumentExceptionInApplyStyleWhenNoPathsAreReturned()
        {
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion); // root path will be non-existent in a PowerShell styled doc.

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            // Act & Assert
            var message = Assert.Throws<ArgumentException>(() => _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument)).Message;
            Assert.Equal("No paths found for the supplied parameters.", message);
        }

        [Theory]
        [InlineData("foo.bar", null)]
        [InlineData(null, "bar.foo")]
        public void ThrowsArgumentExceptionInCreateFilteredDocumentWhenNonExistentOperationIdsAndTagsAreSupplied(string operationIds, string tags)
        {
            var predicate = _openApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: null,
                                                           source: _graphMockSource);

            // Act & Assert
            var message = Assert.Throws<ArgumentException>(() =>
                _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate)).Message;

            Assert.Equal("No paths found for the supplied parameters.", message);
        }

        [Theory]
        [InlineData(null, null, "/users")]
        [InlineData(null, "users.user", null)]
        [InlineData("users.user.ListUser", null, null)]
        public void ReturnValueInCreatePredicateWhenValidArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            // Assert
            Assert.NotNull(predicate);
        }

        [Theory]
        [InlineData(null, null, "/users?$filter=startswith(displayName,'John Doe')")]
        [InlineData(null, "users.user", null)]
        [InlineData(null, "^users.user$", null)]
        [InlineData("users.user.ListUser", null, null)]
        public void ReturnOpenApiDocumentInCreateFilteredDocumentWhenValidArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            // Assert
            Assert.NotNull(subsetOpenApiDocument);

            if (!string.IsNullOrEmpty(operationIds))
            {
                Assert.Single(subsetOpenApiDocument.Paths);
            }
            else if (!string.IsNullOrEmpty(tags))
            {
                Assert.Equal(2, subsetOpenApiDocument.Paths.Count);
            }
            else // url
            {
                Assert.Single(subsetOpenApiDocument.Paths);
            }
        }

        [Theory]
        [InlineData(OpenApiStyle.Plain, "/users/{user-id}", OperationType.Get)]
        [InlineData(OpenApiStyle.Plain, "/users/12345", OperationType.Get)]
        [InlineData(OpenApiStyle.GEAutocomplete, "/users(user-id)messages(message-id)", OperationType.Get)]
        [InlineData(OpenApiStyle.GEAutocomplete, "/users/12345/messages/abcde", OperationType.Get)]
        [InlineData(OpenApiStyle.GEAutocomplete, "/", OperationType.Get)] // root path
        [InlineData(OpenApiStyle.PowerPlatform, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore", OperationType.Post)]
        [InlineData(OpenApiStyle.PowerShell, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore", OperationType.Post, "administrativeUnits_restore")]
        [InlineData(OpenApiStyle.PowerShell, "/users/{user-id}", OperationType.Patch, "users.user_UpdateUser")]
        [InlineData(OpenApiStyle.PowerShell, "/applications/{application-id}/logo", OperationType.Put, "applications.application_SetLogo")]
        public void ReturnStyledOpenApiDocumentInApplyStyleForAllOpenApiStyles(OpenApiStyle style,
                                                                               string url,
                                                                               OperationType operationType,
                                                                               string expectedOperationId = null)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            subsetOpenApiDocument = _openApiService.ApplyStyle(style, subsetOpenApiDocument);

            // Assert
            if (style == OpenApiStyle.GEAutocomplete || style == OpenApiStyle.Plain)
            {
                var content = subsetOpenApiDocument.Paths
                                .FirstOrDefault().Value
                                .Operations[operationType]
                                .Responses["200"]
                                .Content;

                Assert.Single(subsetOpenApiDocument.Paths);

                if (style == OpenApiStyle.GEAutocomplete)
                {
                    Assert.Empty(content);
                }
                else // Plain
                {
                    Assert.NotEmpty(content);
                }
            }
            else // PowerShell || PowerPlatform
            {
                if (operationType == OperationType.Post)
                {
                    var anyOf = subsetOpenApiDocument.Paths
                                    .FirstOrDefault().Value
                                    .Operations[operationType]
                                    .Responses["200"]
                                    .Content["application/json"]
                                    .Schema
                                    .AnyOf;

                    Assert.Null(anyOf);
                }

                if (style == OpenApiStyle.PowerShell)
                {
                    if (operationType == OperationType.Post)
                    {
                        var newOperationId = subsetOpenApiDocument.Paths
                                            .FirstOrDefault().Value
                                            .Operations[operationType]
                                            .OperationId;

                        Assert.Equal(expectedOperationId, newOperationId);
                    }
                    else if (operationType == OperationType.Patch)
                    {
                        var newOperationId = subsetOpenApiDocument.Paths
                                            .FirstOrDefault().Value
                                            .Operations[operationType]
                                            .OperationId;

                        Assert.Equal(expectedOperationId, newOperationId);
                    }
                    else if (operationType == OperationType.Put)
                    {
                        var newOperationId = subsetOpenApiDocument.Paths
                                            .FirstOrDefault().Value
                                            .Operations[operationType]
                                            .OperationId;

                        Assert.Equal(expectedOperationId, newOperationId);
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReturnContentForGEAutoCompleteStyleIfRequestBodyIsTrue(bool includeRequestBody)
        {
            // Arrange
            var style = OpenApiStyle.GEAutocomplete;
            var url = "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore";
            var operationType = OperationType.Post;

            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            subsetOpenApiDocument = _openApiService.ApplyStyle(style, subsetOpenApiDocument, includeRequestBody);
            var requestBodyContent = subsetOpenApiDocument.Paths
                .FirstOrDefault().Value
                .Operations[operationType]
                .RequestBody
                .Content;
            var responseContent = subsetOpenApiDocument.Paths
                                .FirstOrDefault().Value
                                .Operations[operationType]
                                .Responses["200"]
                                .Content;

            // Assert
            Assert.Single(subsetOpenApiDocument.Paths);

            if (includeRequestBody)
            {
                Assert.NotEmpty(requestBodyContent);
                Assert.NotEmpty(responseContent);
            }
            else
            {
                Assert.Empty(requestBodyContent);
                Assert.Empty(responseContent);
            }
        }

        [Fact]
        public void RetrieveAllOperationsAndPaths()
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: "*", // fetch all paths/operations
                                                           tags: null,
                                                           url: null,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.Plain, subsetOpenApiDocument);

            // Assert
            Assert.Equal(23, subsetOpenApiDocument.Paths.Count);
            Assert.NotEmpty(subsetOpenApiDocument.Components.Schemas);
            Assert.NotEmpty(subsetOpenApiDocument.Components.Parameters);
            Assert.NotEmpty(subsetOpenApiDocument.Components.Responses);
            Assert.NotEmpty(subsetOpenApiDocument.Components.RequestBodies);
        }

        [Fact]
        public void EscapePoundCharacterFromNetworkInterfaceSchemaDescription()
        {
            // Arrange
            var expectedDescription = "Description of the NIC (e.g. Ethernet adapter, Wireless LAN adapter Local Area Connection <#/>, etc.).";

            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/security/hostSecurityProfiles",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);

            var parentSchema = subsetOpenApiDocument.Components.Schemas["microsoft.graph.networkInterface"];
            var descriptionSchema = parentSchema.Properties["description"];

            // Assert
            Assert.Equal(expectedDescription, descriptionSchema.Description);
        }

        [Theory]
        [InlineData("/communications/calls/{call-id}/microsoft.graph.keepAlive", OperationType.Post, "communications.calls_keepAlive")]
        [InlineData("/groups/{group-id}/events/{event-id}/calendar/events/microsoft.graph.delta", OperationType.Get, "groups.events.calendar.events_delta")]
        [InlineData("/reports/microsoft.graph.getSharePointSiteUsageDetail(period={period})", OperationType.Get, "reports_getSharePointSiteUsageDetail")]
        public void ResolveActionFunctionOperationIdsForPowerShellStyle(string url, OperationType operationType, string expectedOperationId)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var operationId = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[operationType]
                              .OperationId;

            // Assert
            Assert.Equal(expectedOperationId, operationId);
        }

        [Theory]
        [InlineData("drives.drive.ListDrive", "drive_ListDrive", OperationType.Get)]
        [InlineData("print.taskDefinitions.tasks.GetTrigger", "print.taskDefinition.task_GetTrigger", OperationType.Get)]
        [InlineData("groups.sites.termStore.groups.GetSets", "group.site.termStore.group_GetSet", OperationType.Get)]
        [InlineData("external.industryData.ListDataConnectors", "external.industryData_ListDataConnector", OperationType.Get)]
        [InlineData("applications.application.UpdateLogo", "application_SetLogo", OperationType.Put)]
        [InlineData("identityGovernance.lifecycleWorkflows.workflows.workflow.activate", "identityGovernance.lifecycleWorkflow.workflow_activate", OperationType.Post)]
        public void SingularizeAndDeduplicateOperationIdsForPowerShellStyle(string operationId, string expectedOperationId, OperationType operationType)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: operationId,
                                                           tags: null,
                                                           url: null,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument, singularizeOperationIds: true);
            var singularizedOpId = subsetOpenApiDocument.Paths
                                  .FirstOrDefault().Value
                                  .Operations[operationType]
                                  .OperationId;

            // Assert
            Assert.Equal(expectedOperationId, singularizedOpId);
        }

        [Theory]
        [InlineData("directory.GetDeletedItems.AsApplication", "directory_GetDeletedItemsAsApplication", OperationType.Get)]
        [InlineData("drives.drive.items.driveItem.assignSensitivityLabel", "drives.drive.items.driveItem_assignSensitivityLabel", OperationType.Post)]
        public void ResolveODataCastOperationIdsForPowerShellStyle(string operationId, string expectedOperationId, OperationType operationType)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: operationId,
                                                           tags: null,
                                                           url: null,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var singularizedOpId = subsetOpenApiDocument.Paths
                                  .FirstOrDefault().Value
                                  .Operations[operationType]
                                  .OperationId;

            // Assert
            Assert.Equal(expectedOperationId, singularizedOpId);
        }

        [Fact]
        public void ResolveStructuredAndCollectionValuedFunctionParameters()
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/deviceManagement/microsoft.graph.getRoleScopeTagsByIds(ids={ids})",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var predicate2 = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/reports/microsoft.graph.getSharePointSiteUsageDetail(period={period})",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            var subsetOpenApiDocument2 = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate2);

            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            subsetOpenApiDocument2 = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument2);

            var parameter = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[OperationType.Get]
                              .Parameters
                              .FirstOrDefault();

            var parameter2 = subsetOpenApiDocument2.Paths
                              .FirstOrDefault().Value
                              .Operations[OperationType.Get]
                              .Parameters
                              .FirstOrDefault();

            Assert.NotNull(parameter);
            Assert.NotNull(parameter2);

            var json = parameter.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);
            var json2 = parameter2.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

            var expectedPayload = $@"{{
  ""name"": ""ids"",
  ""in"": ""query"",
  ""description"": ""Usage: ids={{ids}}"",
  ""required"": true,
  ""style"": ""form"",
  ""explode"": false,
  ""schema"": {{
    ""type"": ""array"",
    ""items"": {{
      ""type"": ""string""
    }}
  }}
}}";

            var expectedPayload2 = $@"{{
  ""name"": ""period"",
  ""in"": ""path"",
  ""description"": ""Usage: period={{period}}"",
  ""required"": true,
  ""style"": ""simple"",
  ""schema"": {{
    ""type"": ""string""
  }}
}}";

            // Assert
            Assert.Equal(expectedPayload.ChangeLineBreaks(), json);
            Assert.Equal(expectedPayload2.ChangeLineBreaks(), json2);
        }

        [Theory]
        [InlineData(OpenApiStyle.GEAutocomplete)]
        [InlineData(OpenApiStyle.Plain)]
        [InlineData(OpenApiStyle.PowerPlatform)]
        [InlineData(OpenApiStyle.PowerShell)]
        public void SetExplodePropertyToFalseInParametersWithStyleEqualsFormForAllStyles(OpenApiStyle style)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/users/{user-id}/messages/{message-id}",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(style, subsetOpenApiDocument);

            var parameter = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations.FirstOrDefault().Value
                              .Parameters.First(s => s.Name.Equals("$select"));
            // Assert
            Assert.False(parameter.Explode);
        }

        [Fact]
        public void SetByRefPostfixForRefOperationIds()
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/applications/{application-id}/createdOnBehalfOf/$ref",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var operation = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[OperationType.Get];
            var topParameter = operation.Parameters.FirstOrDefault(p => p.Reference.Id == "top");
            var successResponse = operation.Responses.FirstOrDefault(r => r.Key == "200");

            // Assert
            Assert.Equal("applications_GetCreatedOnBehalfOfByRef", operation.OperationId);
            Assert.Contains(topParameter.Reference.Id, subsetOpenApiDocument.Components.Parameters.Keys);
            Assert.Contains(successResponse.Value.Reference.Id, subsetOpenApiDocument.Components.Responses.Keys);
        }

        [Fact]
        public async Task GetOpenApiTreeNodeAsync()
        {
            // Arrange
            var sources = new ConcurrentDictionary<string, OpenApiDocument>();
            sources.TryAdd(GraphVersion, _graphMockSource);

            // Act
            var rootNode = _openApiService.CreateOpenApiUrlTreeNode(sources);
            using MemoryStream stream = new();
            await ConvertOpenApiUrlTreeNodeToJsonAsync(rootNode, stream);

            // Assert
            var jsonPayload = Encoding.ASCII.GetString(stream.ToArray());
            var expectedPayloadContent = "{\"segment\":\"/\",\"labels\":[{\"name\":\"mock\",\"methods\":[{\"name\":\"Get\",\"documentationUrl\":null}]}],\"children\":[{\"segment\":\"administrativeUnits\",\"labels\":[]," +
                "\"children\":[{\"segment\":\"{administrativeUnit-id}\",\"labels\":[],\"children\":[{\"segment\":\"microsoft.graph.restore\",";

            Assert.NotEmpty(jsonPayload);
            Assert.NotNull(jsonPayload);
            Assert.Contains(expectedPayloadContent, jsonPayload);
        }

        [Theory]
        [InlineData(OpenApiStyle.PowerPlatform)]
        [InlineData(OpenApiStyle.PowerShell)]
        [InlineData(OpenApiStyle.Plain)]
        [InlineData(OpenApiStyle.GEAutocomplete)]
        public void ShowOperationDescriptionsInCreateFilteredDocument(OpenApiStyle style)
        {
            // Arrange
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/users/{user-id}/messages/{message-id}",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);

            // Act
            subsetOpenApiDocument = _openApiService.ApplyStyle(style, subsetOpenApiDocument);
            var description = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[OperationType.Get]
                              .Description;

            // Assert
            Assert.NotNull(description);
        }

        [Fact]
        public void HaveRequestBodyForPostRefOperations()
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/applications/{application-id}/owners/$ref",
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var requestBody = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[OperationType.Post]
                              .RequestBody;

            // Assert
            Assert.Contains(requestBody.Reference.Id, subsetOpenApiDocument.Components.RequestBodies.Keys);
        }

        [Fact]
        public void RemoveAnyOfAndOneOfFromSchemas()
        {
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                               tags: null,
                                               url: "/security/hostSecurityProfiles",
                                               source: _graphMockSource,
                                               graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);

            var averageAudioDegradationProperty = subsetOpenApiDocument.Components.Schemas["microsoft.graph.networkInterface"].Properties["averageAudioDegradation"];
            var defaultPriceProperty = subsetOpenApiDocument.Components.Schemas["microsoft.graph.networkInterface"].Properties["defaultPrice"];

            Assert.Null(averageAudioDegradationProperty.AnyOf);
            Assert.Equal("number", averageAudioDegradationProperty.Type);
            Assert.Equal("float", averageAudioDegradationProperty.Format);
            Assert.True(averageAudioDegradationProperty.Nullable);
            Assert.Null(defaultPriceProperty.OneOf);
            Assert.Equal("number", defaultPriceProperty.Type);
            Assert.Equal("double", defaultPriceProperty.Format);
        }

        [Theory]
        [InlineData("/users/$count", OperationType.Get, "users_GetCount")]
        [InlineData("/reports/microsoft.graph.getSharePointSiteUsageDetail(period={period})", OperationType.Get, "reports_getSharePointSiteUsageDetail")]
        public void RemoveHashSuffixFromOperationIdsForPowerShellStyle(string url, OperationType operationType, string expectedOperationId)
        {
            // Act
            var predicate = _openApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: _graphMockSource,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = _openApiService.CreateFilteredDocument(_graphMockSource, Title, GraphVersion, predicate);
            subsetOpenApiDocument = _openApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var operationId = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[operationType]
                              .OperationId;

            // Assert
            Assert.Equal(expectedOperationId, operationId);
        }

        [Fact]
        public void ConvertOpenApiUrlTreeNodeToJsonRendersExternalDocs()
        {
            // Arrange
            var openApiDocs = new ConcurrentDictionary<string, OpenApiDocument>();
            openApiDocs.TryAdd(GraphVersion, _graphMockSource);
            using MemoryStream stream = new();            
            var writer = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = false });

            // Act
            var rootNode = _openApiService.CreateOpenApiUrlTreeNode(openApiDocs);
            OpenApiService.ConvertOpenApiUrlTreeNodeToJson(writer, rootNode);
            writer.Flush();
            stream.Position = 0;
            var output = new StreamReader(stream).ReadToEnd();

            // Assert
            Assert.Contains("\"children\":[{\"segment\":\"{user-id}\",\"labels\":[{\"name\":\"mock\",\"methods\":[{\"name\":\"Get\",\"documentationUrl\":\"https://docs.microsoft.com/foobar\"}", output);
        }

        private async Task ConvertOpenApiUrlTreeNodeToJsonAsync(OpenApiUrlTreeNode node, Stream stream)
        {
            Assert.NotNull(node);
            await _openApiService.ConvertOpenApiUrlTreeNodeToJsonAsync(node, stream);
            Assert.True(stream.Length > 0);
        }
    }
}
