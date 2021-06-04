// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using System;
using System.Linq;
using Xunit;

namespace OpenAPIService.Test
{
    public class OpenAPIServiceShould
    {
        private const string Title = "Partial Graph API";
        private const string GraphVersion = "beta";
        private readonly OpenApiDocument _graphBetaSource = null;

        public OpenAPIServiceShould()
        {
            // Create OpenAPI document with default OpenApiStyle = Plain
            _graphBetaSource = OpenAPIDocumentCreatorMock.GetGraphOpenApiDocument("Beta", false);
        }

        [Fact]
        public void FormatPathFunctionsOfStringDataTypesWithSingleQuotationMarks()
        {
            // Arrange
            string operationId_1 = "reports.getTeamsUserActivityCounts";
            string operationId_2 = "reports.getTeamsUserActivityUserDetail-a3f1";
            OpenApiDocument source = _graphBetaSource;

            var predicate_1 = OpenApiService.CreatePredicate(operationIds: operationId_1,
                                                             tags: null,
                                                             url: null,
                                                             source: source);

            var predicate_2 = OpenApiService.CreatePredicate(operationIds: operationId_2,
                                                             tags: null,
                                                             url: null,
                                                             source: source);

            // Act
            var subsetOpenApiDocument_1 = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate_1);
            var subsetOpenApiDocument_2 = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate_2);

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
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act and Assert
            if (string.IsNullOrEmpty(operationIds) &&
                string.IsNullOrEmpty(tags) &&
                string.IsNullOrEmpty(url))
            {
                var message = Assert.Throws<InvalidOperationException>(() => 
                    OpenApiService.CreatePredicate(operationIds: operationIds,
                                                   tags: tags,
                                                   url: url,
                                                   source: source)).Message;
                Assert.Equal("Either operationIds, tags or url need to be specified.", message);
            }
            else
            {
                var message = Assert.Throws<InvalidOperationException>(() => 
                    OpenApiService.CreatePredicate(operationIds: operationIds,
                                                   tags: tags,
                                                   url: url,
                                                   source: source)).Message;

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
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act and Assert
            var message = Assert.Throws<ArgumentException>(() => OpenApiService.CreatePredicate(operationIds: null,
                                                                                                tags: null,
                                                                                                url: "/foo",
                                                                                                source: source,
                                                                                                graphVersion: GraphVersion)).Message;
            Assert.Equal("The url supplied could not be found.", message);
        }

        [Fact]
        public void ThrowsArgumentExceptionInApplyStyleWhenNoPathsAreReturned()
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            var predicate = OpenApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/",
                                                           source: source,
                                                           graphVersion: GraphVersion); // root path will be non-existent in a PowerShell styled doc.

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

            // Act & Assert
            var message = Assert.Throws<ArgumentException>(() => OpenApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument)).Message;
            Assert.Equal("No paths found for the supplied parameters.", message);
        }

        [Theory]
        [InlineData("foo.bar", null)]
        [InlineData(null, "bar.foo")]
        public void ThrowsArgumentExceptionInCreateFilteredDocumentWhenNonExistentOperationIdsAndTagsAreSupplied(string operationIds, string tags)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            var predicate = OpenApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: null,
                                                           source: source);

            // Act & Assert
            var message = Assert.Throws<ArgumentException>(() =>
                OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate)).Message;

            Assert.Equal("No paths found for the supplied parameters.", message);
        }

        [Theory]
        [InlineData(null, null, "/users")]
        [InlineData(null, "users.user", null)]
        [InlineData("users.user.ListUser", null, null)]
        public void ReturnValueInCreatePredicateWhenValidArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: url,
                                                           source: source,
                                                           graphVersion: GraphVersion);

            // Assert
            Assert.NotNull(predicate);
        }

        [Theory]
        [InlineData(null, null, "/users?$filter=startswith(displayName,'John Doe')")]
        [InlineData(null, "users.user", null)]
        [InlineData("users.user.ListUser", null, null)]
        public void ReturnOpenApiDocumentInCreateFilteredDocumentWhenValidArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: operationIds,
                                                           tags: tags,
                                                           url: url,
                                                           source: source,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

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
        [InlineData(OpenApiStyle.PowerPlatform, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore", OperationType.Post)]
        [InlineData(OpenApiStyle.PowerShell, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore", OperationType.Post, "administrativeUnits_restore")]
        [InlineData(OpenApiStyle.PowerShell, "/users/{user-id}", OperationType.Patch, "users.user_UpdateUser")]
        [InlineData(OpenApiStyle.PowerShell, "/applications/{application-id}/logo", OperationType.Put, "applications.application_SetLogo")]
        public void ReturnStyledOpenApiDocumentInApplyStyleForAllOpenApiStyles(OpenApiStyle style,
                                                                               string url,
                                                                               OperationType operationType,
                                                                               string expectedOperationId = null)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: source,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

            subsetOpenApiDocument = OpenApiService.ApplyStyle(style, subsetOpenApiDocument);

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

        [Fact]
        public void RemoveRootPathFromOpenApiDocumentInApplyStyleForPowerShellOpenApiStyle()
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: "*",
                                                           tags: null,
                                                           url: null,
                                                           source: source); // fetch all paths/operations

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

            subsetOpenApiDocument = OpenApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);

            // Assert
            Assert.False(subsetOpenApiDocument.Paths.ContainsKey("/")); // root path
        }

        [Fact]
        public void EscapePoundCharacterFromNetworkInterfaceSchemaDescription()
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;
            var expectedDescription = "Description of the NIC (e.g. Ethernet adapter, Wireless LAN adapter Local Area Connection <#/>, etc.).";

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: "/security/hostSecurityProfiles",
                                                           source: source,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);
            subsetOpenApiDocument = OpenApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);

            var parentSchema = subsetOpenApiDocument.Components.Schemas["microsoft.graph.networkInterface"];
            var descriptionSchema = parentSchema.Properties["description"];

            // Assert
            Assert.Equal(expectedDescription, descriptionSchema.Description);
        }

        [Theory]
        [InlineData("/communications/calls/{call-id}/microsoft.graph.keepAlive", OperationType.Post, "communications.calls_keepAlive")]
        [InlineData("/groups/{group-id}/events/{event-id}/calendar/events/microsoft.graph.delta", OperationType.Get, "groups.events.calendar.events_delta")]
        public void ResolveActionFunctionOperationIdsForPowerShellStyle(string url, OperationType operationType, string expectedOperationId)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: null,
                                                           tags: null,
                                                           url: url,
                                                           source: source,
                                                           graphVersion: GraphVersion);

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);
            subsetOpenApiDocument = OpenApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);
            var operationId = subsetOpenApiDocument.Paths
                              .FirstOrDefault().Value
                              .Operations[operationType]
                              .OperationId;

            // Assert
            Assert.Equal(expectedOperationId, operationId);
        }
    }
}
