// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Xunit;

namespace UtilityService.Test
{
    public class UtilityServiceShould
    {
        [Theory]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("/openapi", "/openapi")]
        [InlineData("/openapi?url=/me/messages", "/openapi")]
        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=startswith(displayName,'John Doe')",
                    "https://graphexplorerapi.azurewebsites.net/openapi")]
        public void GetBaseUriPath(string targetUri, string expectedBasePath)
        {
            // Arrange and Act
            var actualBasePath = targetUri.BaseUriPath();

            // Assert
            Assert.Equal(expectedBasePath, actualBasePath);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData(null, null)]
        [InlineData("/openapi", "")]
        [InlineData("/openapi?url=/me/messages", "url=/me/messages")]
        [InlineData("https://graphexplorerapi.azurewebsites.net/openapi?url=/users?$filter=startswith(displayName,'John Doe')",
                    "url=/users?$filter=startswith(displayName,'John Doe')")]
        public void GetUriQuery(string targetUri, string expectedQuery)
        {
            // Arrange and Act
            var actualQuery = targetUri.Query();

            // Assert
            Assert.Equal(expectedQuery, actualQuery);
        }
    }
}
