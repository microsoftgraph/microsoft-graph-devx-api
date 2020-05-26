// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.OpenApi.Models;
using OpenAPIService.Common;
using Xunit;

namespace OpenAPIService.Test
{
    public class OpenAPIServiceShould
    {
        private const string GraphBetaCsdl = ".\\TestFiles\\Graph.Beta.OData.xml";
        private const string Title = "Partial Graph API";
        private readonly OpenApiDocument _source = null;

        public OpenAPIServiceShould()
        {
            // Create OpenAPI document with default OpenApiStyle = Plain
            _source = OpenAPIDocumentCreatorMock.CreateOpenApiDocument(GraphBetaCsdl);
        }

        [Fact]
        public void FormatPathFunctionsOfStringDataTypesWithSingleQuotationMarks()
        {
            // Arrange
            string operationId_1 = "reports.getTeamsUserActivityCounts";
            string operationId_2 = "reports.getTeamsUserActivityUserDetail-a3f1";
            string graphVersion = "beta";
            OpenApiDocument source = _source;
            OpenApiStyleOptions styleOptions = new OpenApiStyleOptions(OpenApiStyle.PowerShell, graphVersion: graphVersion);
            var predicate_1 = OpenApiService.CreatePredicate(operationId_1, null, null, graphVersion, false).GetAwaiter().GetResult();
            var predicate_2 = OpenApiService.CreatePredicate(operationId_2, null, null, graphVersion, false).GetAwaiter().GetResult();

            // Act
            var subsetOpenApiDocument_1 = OpenApiService.CreateFilteredDocument(source, Title, styleOptions, predicate_1);
            var subsetOpenApiDocument_2 = OpenApiService.CreateFilteredDocument(source, Title, styleOptions, predicate_2);

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
    }
}
