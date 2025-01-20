using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class PowerShellGeneratorTests : OpenApiSnippetGeneratorTestBase
    {
        private readonly PowerShellGenerator _generator = new(GetMgCommandMetadataAsync().GetAwaiter().GetResult());

        [Fact]
        public async Task GeneratesSnippetForTheGetMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePostMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("New-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePatchMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Update-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForThePutMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/applications/{{application-id}}/logo");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForTheDeleteMethodCallAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Delete, $"{ServiceRootUrl}/me/messages/{{message-id}}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Remove-", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSelectQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$select=displayName,id");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Property \"displayName,id\"", result);
        }

        [Fact]
        public async Task GeneratesNoneEncodedSnippetForRequestWithFilterQueryOptionAndEncodedPayloadsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=displayName+eq+'Megan+Bowen'");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Filter \"displayName eq 'Megan Bowen'\"", result);
        }


        [Fact]
        public async Task GeneratesSnippetForRequestWithFilterQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$filter=displayName eq 'Megan Bowen'");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Filter \"displayName eq 'Megan Bowen'\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithExpandQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-ExpandProperty \"members\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithExpandQueryOptionWithSelectAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/groups?$expand=members($select=displayName)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-ExpandProperty \"members(`$select=displayName)\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithOrderByQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$orderby=displayName");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Sort \"displayName\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithOrderByQueryOptionWithSortDirectionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$orderby=displayName desc");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Sort \"displayName desc\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithOrderByQueryOptionWithMultipleSortingFieldsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$orderby=displayName,mail");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Sort \"displayName,mail\"", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithTopQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$top=3");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Top 3", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSkipQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$skip=10");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Skip 10", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithCountQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$count=true");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-CountVariable CountVar", result);
            Assert.Contains("-ConsistencyLevel eventual", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithPathParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users/48d31887-5fad-4d73-a9f5-3c356e68a038/appRoleAssignments/hxjTSK1fc02p9Tw1bmigOD83KvI8C1hIl2enozuqhEM");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-UserId $userId -AppRoleAssignmentId $appRoleAssignmentId", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithQueryParametersAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?customParameter=value");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Customparameter \"value\"", result);
        }

        [Theory]
        [InlineData("/me")]
        [InlineData("/me/messages")]
        public async Task GeneratesSnippetsForMeSegmentAsync(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-UserId $userId", result);
            Assert.Contains("# A UPN can also be used as -UserId.", result);
        }

        [Theory]
        [InlineData("/users")]
        [InlineData("/users/48d31887-5fad-4d73-a9f5-3c356e68a038")]
        public async Task GeneratesSnippetsForUserSegmentAsync(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);

            Assert.Contains("-MgUser", result);
            Assert.DoesNotContain("# A UPN can also be used as -UserId.", result);
        }

        [Theory]
        [InlineData("/me/changePassword")]
        [InlineData("/users/{user-id}/microsoft.graph.changePassword")]
        public async Task GeneratesSnippetsForODataActionAsync(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-UserId $userId", result);
            Assert.Contains("Update-MgUserPassword", result);
        }

        [Theory]
        [InlineData("/me")]
        [InlineData("/users/48d31887-5fad-4d73-a9f5-3c356e68a038")]
        public async Task GeneratesSnippetForImportModuleAsync(string path)
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}{path}");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var firstLine = result.Substring(0, result.IndexOf(Environment.NewLine));
            Assert.StartsWith("Import-Module Microsoft.Graph.Users", firstLine);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOptionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:Megan\"");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Search '\"displayName:Megan\"'", result);
            Assert.Contains("-ConsistencyLevel eventual", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOptionWithORLogicalConjuctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:di\" OR \"displayName:al\"");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Search '\"displayName:di\" OR \"displayName:al\"'", result);
            Assert.Contains("-ConsistencyLevel eventual", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithSearchQueryOptionWithANDLogicalConjuctionAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users?$search=\"displayName:di\" AND \"displayName:al\"");
            requestPayload.Headers.Add("ConsistencyLevel", "eventual");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-Search '\"displayName:di\" AND \"displayName:al\"'", result);
            Assert.Contains("-ConsistencyLevel eventual", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithNoBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/users");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.DoesNotContain("-BodyParameter", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(
                    "{\"displayName\":\"Melissa Darrow\",\"city\":\"Seattle\"}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"displayName = \"Melissa Darrow\"{Environment.NewLine}\t" +
                $"city = \"Seattle\"{Environment.NewLine}}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithNestedBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/users")
            {
                Content = new StringContent(
                    "{\"displayName\":\"Melissa Darrow\"," +
                    "\"city\":\"Seattle\"," +
                    "\"PasswordProfile\":{" +
                    "\"Password\":\"2d79ba3a-b03a-9ed5-86dc-79544e262664\"," +
                    "\"ForceChangePasswordNextSignIn\": false}}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"displayName = \"Melissa Darrow\"{Environment.NewLine}\t" +
                $"city = \"Seattle\"{Environment.NewLine}\t" +
                $"PasswordProfile = @{{{Environment.NewLine}\t\t" +
                $"Password = \"2d79ba3a-b03a-9ed5-86dc-79544e262664\"{Environment.NewLine}\t\t" +
                $"ForceChangePasswordNextSignIn = $false{Environment.NewLine}\t" +
                $"}}{Environment.NewLine}" +
                $"}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithArrayInBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/groups")
            {
                Content = new StringContent(
                    "{\"displayName\": \"Library Assist\",\"groupTypes\": [\"Unified\",\"DynamicMembership\"]}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"displayName = \"Library Assist\"{Environment.NewLine}\t" +
                $"groupTypes = @({Environment.NewLine}\t" +
                $"\"Unified\"{Environment.NewLine}" +
                $"\"DynamicMembership\"{Environment.NewLine}" +
                $"){Environment.NewLine}" +
                $"}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithWrongQuotesForStringLiteralsInBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Post, $"{ServiceRootUrl}/policies/claimsMappingPolicies")
            {
                Content = new StringContent(
                    "{\r\n    \"definition\": [\r\n        \"{\\\"ClaimsMappingPolicy\\\":{\\\"Version\\\":1,\\\"IncludeBasicClaimSet\\\":\\\"true\\\", \\\"ClaimsSchema\\\": [{\\\"Source\\\":\\\"user\\\",\\\"ID\\\":\\\"assignedroles\\\",\\\"SamlClaimType\\\": \\\"https://aws.amazon.com/SAML/Attributes/Role\\\"}, {\\\"Source\\\":\\\"user\\\",\\\"ID\\\":\\\"userprincipalname\\\",\\\"SamlClaimType\\\": \\\"https://aws.amazon.com/SAML/Attributes/RoleSessionName\\\"}, {\\\"Value\\\":\\\"900\\\",\\\"SamlClaimType\\\": \\\"https://aws.amazon.com/SAML/Attributes/SessionDuration\\\"}, {\\\"Source\\\":\\\"user\\\",\\\"ID\\\":\\\"assignedroles\\\",\\\"SamlClaimType\\\": \\\"appRoles\\\"}, {\\\"Source\\\":\\\"user\\\",\\\"ID\\\":\\\"userprincipalname\\\",\\\"SamlClaimType\\\": \\\"https://aws.amazon.com/SAML/Attributes/nameidentifier\\\"}]}}\"\r\n    ],\r\n    \"displayName\": \"AWS Claims Policy\",\r\n    \"isOrganizationDefault\": false\r\n}",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"definition = @(" +
                $"{Environment.NewLine}\t" +
                $"'{{\"ClaimsMappingPolicy\":{{\"Version\":1,\"IncludeBasicClaimSet\":\"true\", \"ClaimsSchema\": [{{\"Source\":\"user\",\"ID\":\"assignedroles\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/Role\"}}, {{\"Source\":\"user\",\"ID\":\"userprincipalname\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/RoleSessionName\"}}, {{\"Value\":\"900\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/SessionDuration\"}}, {{\"Source\":\"user\",\"ID\":\"assignedroles\",\"SamlClaimType\": \"appRoles\"}}, {{\"Source\":\"user\",\"ID\":\"userprincipalname\",\"SamlClaimType\": \"https://aws.amazon.com/SAML/Attributes/nameidentifier\"}}]}}}}'{Environment.NewLine}" +
                $")" +
                $"{Environment.NewLine}" +
                $"displayName = \"AWS Claims Policy\"{Environment.NewLine}" +
                $"isOrganizationDefault = $false{Environment.NewLine}" +
                $"}}";
            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }

        [Fact]
        public async Task GeneratesSnippetForDeltaFunctionsWithoutParamsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/contacts/delta()?$select=displayName%2CjobTitle%2Cmail");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgContactDelta", result);
        }

        [Fact]
        public async Task GeneratesSnippetForHttpSnippetsWithGraphPrefixOnLastPathSegmentAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/places/graph.room");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgPlaceAsRoom", result);
        }

        [Fact]
        public async Task GeneratesSnippetForHttpSnippetsWithoutGraphPrefixOnLastPathSegmentAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/places/microsoft.graph.room");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgPlaceAsRoom", result);
        }

        [Fact]
        public async Task GeneratesSnippetForPathsWithIdentityProviderAsRootNodeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootUrl}/identityProviders");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgIdentityProvider", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithTextContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/me/drive/items/XXXX/content")
            {
                Content = new StringContent(
                    "Plain text",
                    Encoding.UTF8,
                    "text/plain")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgDriveItemContent", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithApplicationZipContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/me/drive/items/XXXX/content")
            {
                Content = new StringContent(
                    "Zip file content",
                    Encoding.UTF8,
                    "application/zip")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgDriveItemContent", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithImageContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootUrl}/me/photo/$value")
            {
                Content = new StringContent(
                    "Binary data for the image",
                    Encoding.UTF8,
                    "image/jpeg")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgUserPhotoContent", result);
            Assert.Contains("-BodyParameter", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForFunctionsWithoutParamsAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/contacts/delta()?$select=displayName%2CjobTitle%2Cmail");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgBetaContactDelta", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForFunctionsWithSingleParamAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/roleManagement/directory/roleEligibilitySchedules/filterByCurrentUser(on='principal')");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Invoke-MgBetaFilterRoleManagementDirectoryRoleEligibilityScheduleByCurrentUser", result);
            Assert.Contains("-On", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForFunctionsWithMultipleParamAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/communications/callRecords/getPstnBlockedUsersLog(fromDateTime=XXXXXX,toDateTime=XXXXX)");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgBetaCommunicationCallRecordPstnBlockedUserLog", result);
            Assert.Contains("-ToDateTime", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForHttpSnippetsWithGraphPrefixOnLastPathSegmentAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/places/graph.room");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgBetaPlaceAsRoom", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForPathsWithIdentityProviderAsRootNodeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Get, $"{ServiceRootBetaUrl}/identityProviders");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Get-MgBetaIdentityProvider", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForRequestWithTextContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootBetaUrl}/me/drive/items/XXXX/content")
            {
                Content = new StringContent(
                    "Plain text",
                    Encoding.UTF8,
                    "text/plain")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgBetaDriveItemContent", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForRequestWithApplicationZipContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootBetaUrl}/me/drive/items/XXXX/content")
            {
                Content = new StringContent(
                    "Zip file content",
                    Encoding.UTF8,
                    "application/zip")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgBetaDriveItemContent", result);
        }

        [Fact]
        public async Task GeneratesBetaSnippetForRequestWithImageContentTypeAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Put, $"{ServiceRootBetaUrl}/me/photo/$value")
            {
                Content = new StringContent(
                    "Binary data for the image",
                    Encoding.UTF8,
                    "image/jpeg")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("Set-MgBetaUserPhotoContent", result);
            Assert.Contains("-BodyParameter", result);
        }

        [Fact]
        public async Task GeneratesSnippetForRequestWithHyphenatedRequestHeaderNamesAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootBetaUrl}/planner/tasks/xxxxxxxx");
            requestPayload.Headers.Add("If-Match", "W/\"lastEtagId\"");
            var snippetModel = new SnippetModel(requestPayload, ServiceRootBetaUrl, await GetBetaSnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            Assert.Contains("-IfMatch W/'\"lastEtagId\"'", result);
            Assert.Contains("Update-MgBetaPlannerTask", result);
        }
        [Fact]
        public async Task GeneratesSnippetForRequestWithNestedArrayAndObjectsInBodyAsync()
        {
            using var requestPayload = new HttpRequestMessage(HttpMethod.Patch, $"{ServiceRootUrl}/education/classes/XXXXX/assignmentSettings")
            {
                Content = new StringContent(
                    "{ \"gradingSchemes\": [ { \"displayName\": \"Pass/fail\", \"grades\": [ { \"displayName\": \"Pass\", \"minPercentage\": 60, \"defaultPercentage\": 100 }, { \"displayName\": \"Fail\", \"minPercentage\": 0, \"defaultPercentage\": 0 } ] }, { \"displayName\": \"Letters\", \"grades\": [ { \"displayName\": \"A\", \"minPercentage\": 90 }, { \"displayName\": \"B\", \"minPercentage\": 80 }, { \"displayName\": \"C\", \"minPercentage\": 70 }, { \"displayName\": \"D\", \"minPercentage\": 60 }, { \"displayName\": \"F\", \"minPercentage\": 0 } ] } ] }",
                    Encoding.UTF8,
                    "application/json")
            };
            var snippetModel = new SnippetModel(requestPayload, ServiceRootUrl, await GetV1SnippetMetadataAsync());
            await snippetModel.InitializeModelAsync(requestPayload);
            var result = _generator.GenerateCodeSnippet(snippetModel);
            var expectedParams = $"$params = @{{{Environment.NewLine}\t" +
                $"gradingSchemes = @({Environment.NewLine}\t\t" +
                $"@{{{Environment.NewLine}\t\t\t" +
                $"displayName = \"Pass/fail\"{Environment.NewLine}\t\t\t" +
                $"grades = @({Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"Pass\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t\t" +
                $"defaultPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"Fail\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t\t" +
                $"defaultPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t" +
                $"){Environment.NewLine}\t\t" +
                $"}}{Environment.NewLine}\t\t" +
                $"@{{{Environment.NewLine}\t\t\t" +
                $"displayName = \"Letters\"{Environment.NewLine}\t\t\t" +
                $"grades = @({Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"A\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"B\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"C\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"D\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t\t" +
                $"@{{{Environment.NewLine}\t\t\t\t\t" +
                $"displayName = \"F\"{Environment.NewLine}\t\t\t\t\t" +
                $"minPercentage = {Environment.NewLine}\t\t\t\t" +
                $"}}{Environment.NewLine}\t\t\t" +
                $"){Environment.NewLine}\t\t" +
                $"}}{Environment.NewLine}\t" +
                $"){Environment.NewLine}" +
                $"}}";

            Assert.Contains(expectedParams, result);
            Assert.Contains("-BodyParameter $params", result);
        }
    }

}
