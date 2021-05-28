// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Xunit;

namespace UtilityService.Test
{
    public class UtilityServiceExtensionsShould
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

        [Theory]
        [InlineData("microsoft.graph.delta()",
                    "microsoft.graph.delta")]
        [InlineData("worksheets/microsoft.graph.range(address={address})",
                    "worksheets/microsoft.graph.range")]
        [InlineData("worksheets/microsoft.graph.range(address={address})/microsoft.graph.range(address={address})",
                    "worksheets/microsoft.graph.range/microsoft.graph.range")]
        public void RemoveParantheses(string targetString, string expectedString)
        {
            // Arrange and Act
            var actualString = targetString.RemoveParentheses();

            // Assert
            Assert.Equal(expectedString, actualString);
        }

        [Theory]
        [InlineData("/users('MeganB@M365x214355.onmicrosoft.com')",
                    "/users/'MeganB@M365x214355.onmicrosoft.com'")]
        [InlineData("/education/schools(id)users/microsoft.graph.delta()",
                    "/education/schools/id/users/delta")]
        [InlineData("/users/{user-id}/insights/used(usedInsight-id)resource/microsoft.graph.workbookWorksheet/microsoft.graph.range(address={address})",
                    "/users/{user-id}/insights/used/usedInsight-id/resource/workbookWorksheet/range")]
        [InlineData("/groupLifecyclePolicies(groupLifecyclePolicy-id)microsoft.graph.remove",
                    "/groupLifecyclePolicies/groupLifecyclePolicy-id/remove")]
        [InlineData("worksheets/range/microsoft.graph.range(address={address})",
                    "worksheets/range/range")]
        [InlineData("/permissions?requesturl=/students('MeganB@M365x214355.onmicrosoft.com')/classes",
                    "/permissions?requesturl=/students/'MeganB@M365x214355.onmicrosoft.com'/classes")]
        [InlineData("/permissions?requesturl=/students/('MeganB@M365x214355.onmicrosoft.com')/classes",
                    "/permissions?requesturl=/students/'MeganB@M365x214355.onmicrosoft.com'/classes")]
        public void GetUriTemplatePathFormat(string targetString, string expectedString)
        {
            // Arrange and Act
            var actualString = targetString.UriTemplatePathFormat();

            // Assert
            Assert.Equal(expectedString, actualString);
        }
    }
}
