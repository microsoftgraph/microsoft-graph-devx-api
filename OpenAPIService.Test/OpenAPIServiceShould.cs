// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using System;
using Xunit;

namespace OpenAPIService.Test
{
    public class OpenAPIServiceShould
    {
        private const string GraphBetaCsdl = ".\\TestFiles\\Graph.Beta.OData.xml";
        private const string Title = "Partial Graph API";
        private const string GraphVersion = "beta";
        private readonly OpenApiDocument _graphBetaSource = null;

        public OpenAPIServiceShould()
        {
            // Create OpenAPI document with default OpenApiStyle = Plain
            _graphBetaSource = OpenAPIDocumentCreatorMock.GetGraphOpenApiDocument(GraphBetaCsdl, false);
        }

        [Fact]
        public void ReturnAllPathsInGraphCsdl()
        {
            Assert.Equal(4586, _graphBetaSource.Paths.Count);
            Assert.NotNull(_graphBetaSource.Paths["/"]); // root path
        }

        [Fact]
        public void FormatPathFunctionsOfStringDataTypesWithSingleQuotationMarks()
        {
            // Arrange
            string operationId_1 = "reports.getTeamsUserActivityCounts";
            string operationId_2 = "reports.getTeamsUserActivityUserDetail-a3f1";
            OpenApiDocument source = _graphBetaSource;

            var predicate_1 = OpenApiService.CreatePredicate(operationIds: operationId_1, tags: null, url: null, source: source)
                .GetAwaiter().GetResult();

            var predicate_2 = OpenApiService.CreatePredicate(operationIds: operationId_2, tags: null, url: null, source: source)
                .GetAwaiter().GetResult();

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
        public void ThrowsArgumentExceptionInCreatePredicateWhenInvalidNumberOfArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act and Assert
            if (string.IsNullOrEmpty(operationIds) &&
                string.IsNullOrEmpty(tags) &&
                string.IsNullOrEmpty(url))
            {
                Assert.Throws<ArgumentNullException>(() => OpenApiService.CreatePredicate(operationIds: operationIds, tags: tags, url: url, source: source)
                .GetAwaiter().GetResult());
            }
            else
            {
                Assert.Throws<ArgumentException>(() => OpenApiService.CreatePredicate(operationIds: operationIds, tags: tags, url: url, source: source)
                                .GetAwaiter().GetResult());
            }
        }

        [Fact]
        public void ThrowsArgumentExceptionInCreatePredicateWhenInvalidUrlArgumentIsSpecified()
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act and Assert
            Assert.Throws<ArgumentException>(() => OpenApiService.CreatePredicate(operationIds: null, tags: null, url: "foo", source: source)
                                .GetAwaiter().GetResult());
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
            var predicate = OpenApiService.CreatePredicate(operationIds: operationIds, tags: tags, url: url, source: source)
                                .GetAwaiter().GetResult();

            // Assert
            Assert.NotNull(predicate);
        }

        [Theory]
        [InlineData(null, null, "/users")]
        [InlineData(null, "users.user", null)]
        [InlineData("users.user.ListUser", null, null)]
        public void ReturnOpenApiDocumentInCreateFilteredDocumentWhenValidArgumentsAreSpecified(string operationIds, string tags, string url)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: operationIds, tags: tags, url: url, source: source)
                                .GetAwaiter().GetResult();

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
        [InlineData(OpenApiStyle.Plain, "/users/{user-id}")]
        [InlineData(OpenApiStyle.GEAutocomplete, "/users/{user-id}")]
        [InlineData(OpenApiStyle.PowerShell, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore")]
        [InlineData(OpenApiStyle.PowerPlatform, "/administrativeUnits/{administrativeUnit-id}/microsoft.graph.restore")]
        public void ReturnOpenApiDocumentInApplyStyleForAllOpenApiStyles(OpenApiStyle style, string url)
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: null, tags: null, url: url, source: source)
                                .GetAwaiter().GetResult();

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

            subsetOpenApiDocument = OpenApiService.ApplyStyle(style, subsetOpenApiDocument);

            // Assert
            if (style == OpenApiStyle.GEAutocomplete || style == OpenApiStyle.Plain)
            {
                var content = subsetOpenApiDocument.Paths[url]
                    .Operations[OperationType.Get].Responses["200"].Content;

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
                var anyOf = subsetOpenApiDocument.Paths[url]
                    .Operations[OperationType.Post].Responses["200"].Content["application/json"].Schema.AnyOf;

                Assert.Null(anyOf);

                if (style == OpenApiStyle.PowerShell)
                {
                    var newOperationId = subsetOpenApiDocument.Paths[url]
                    .Operations[OperationType.Post].OperationId;

                    Assert.Equal("administrativeUnits_restore", newOperationId);
                }
            }
        }

        [Fact]
        public void RemoveRootPathFromOpenApiDocumentInApplyStyleForPowerShellOpenApiStyle()
        {
            // Arrange
            OpenApiDocument source = _graphBetaSource;

            // Act
            var predicate = OpenApiService.CreatePredicate(operationIds: "*", tags: null, url: null, source: source)
                                .GetAwaiter().GetResult(); // fetch all paths/operations

            var subsetOpenApiDocument = OpenApiService.CreateFilteredDocument(source, Title, GraphVersion, predicate);

            subsetOpenApiDocument = OpenApiService.ApplyStyle(OpenApiStyle.PowerShell, subsetOpenApiDocument);

            // Assert
            Assert.Equal(4585, subsetOpenApiDocument.Paths.Count);
            Assert.False(subsetOpenApiDocument.Paths.ContainsKey("/")); // root path
        }
    }
}
