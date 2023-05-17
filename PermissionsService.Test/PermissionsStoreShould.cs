// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using MockTestUtility;
using PermissionsService.Interfaces;
using PermissionsService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PermissionsService.Test
{
    public class PermissionsStoreShould
    {
        private readonly IPermissionsStore _permissionsStore;
        private static string ConfigFilePath => Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest-valid.json");

        public PermissionsStoreShould()
        {
            _permissionsStore = PermissionStoreFactoryMock.GetPermissionStore(ConfigFilePath);
        }

        [Fact]
        public async Task GetAllRequiredPermissionScopesGivenAnExistingRequestUrl()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                requests: new List<RequestInfo> { new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" } });

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                    Assert.Equal("Read your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events without a signed-in user.", item.Description);
                    Assert.Equal(ScopeType.Application, item.ScopeType);
                    Assert.False(item.IsAdmin);
                    Assert.True(item.IsLeastPrivilege);
                    Assert.False(item.IsHidden);
                },
                item =>
                {
                    Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                    Assert.Equal("Read and update your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events without a signed-in user. Also allows the app to update editable properties in security events.", item.Description);
                    Assert.Equal(ScopeType.Application, item.ScopeType);
                    Assert.False(item.IsAdmin);
                    Assert.False(item.IsLeastPrivilege);
                    Assert.False(item.IsHidden);
                },
                item =>
                {
                    Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                    Assert.Equal("Read your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events on your behalf.", item.Description);
                    Assert.Equal(ScopeType.DelegatedWork, item.ScopeType);
                    Assert.True(item.IsAdmin);
                    Assert.True(item.IsLeastPrivilege);
                    Assert.False(item.IsHidden);
                },
                item =>
                {
                    Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                    Assert.Equal("Read and update your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events on your behalf. Also allows you to update editable properties in security events.", item.Description);
                    Assert.Equal(ScopeType.DelegatedWork, item.ScopeType);
                    Assert.True(item.IsAdmin);
                    Assert.False(item.IsLeastPrivilege);
                    Assert.False(item.IsHidden);
                });
        }

        [Theory]
        [InlineData(ScopeType.DelegatedWork)]
        [InlineData(ScopeType.DelegatedPersonal)]
        [InlineData(ScopeType.Application)]
        public async Task GetRequiredPermissionScopesGivenAnExistingRequestUrlByScopeType(ScopeType scopeType)
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" } },
                scopeType: scopeType);

            // Assert
            if (scopeType == ScopeType.DelegatedWork)
            {
                // Assert
                Assert.Collection(result.Results,
                    item =>
                    {
                        Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                        Assert.Equal("Read your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events on your behalf.", item.Description);
                        Assert.Equal(ScopeType.DelegatedWork, item.ScopeType);
                        Assert.True(item.IsAdmin);
                        Assert.True(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    },
                    item =>
                    {
                        Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                        Assert.Equal("Read and update your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events on your behalf. Also allows you to update editable properties in security events.", item.Description);
                        Assert.Equal(ScopeType.DelegatedWork, item.ScopeType);
                        Assert.True(item.IsAdmin);
                        Assert.False(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    });
            }
            else if (scopeType == ScopeType.Application)
            {
                // Assert
                Assert.Collection(result.Results,
                    item =>
                    {
                        Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                        Assert.Equal("Read your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events without a signed-in user.", item.Description);
                        Assert.Equal(ScopeType.Application, item.ScopeType);
                        Assert.False(item.IsAdmin);
                        Assert.True(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    },
                    item =>
                    {
                        Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                        Assert.Equal("Read and update your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events without a signed-in user. Also allows the app to update editable properties in security events.", item.Description);
                        Assert.Equal(ScopeType.Application, item.ScopeType);
                        Assert.False(item.IsAdmin);
                        Assert.False(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    });
            }
            else
            {
                Assert.Null(result.Results);
            }
        }

        [Fact]
        public async Task GetLeastPrivilegePermissionScopesGivenAnExistingRequestUrl()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" } },
                scopeType: ScopeType.Application, 
                leastPrivilegeOnly: true);

            // Assert
            Assert.Single(result.Results);
            Assert.Collection(result.Results,
                    item =>
                    {
                        Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                        Assert.Equal("Read your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events without a signed-in user.", item.Description);
                        Assert.Equal(ScopeType.Application, item.ScopeType);
                        Assert.False(item.IsAdmin);
                        Assert.True(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    });
        }

        [Theory]
        [InlineData(ScopeType.DelegatedWork, 311)]
        [InlineData(ScopeType.DelegatedPersonal, 113)]
        [InlineData(ScopeType.Application, 279)]
        public async Task GetAllPermissionScopesGivenNoRequestUrlFilteredByScopeType(ScopeType scopeType, int expectedCount)
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(scopeType: scopeType);

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(expectedCount, result.Results.Count);
        }

        [Fact]
        public async Task GetAllPermissionScopesGivenNoRequestUrl()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync();

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(625, result.Results.Count);
        }

        [Fact]
        public async Task ReturnNullGivenANonExistentRequestUrl()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/foo/bar/{id}", HttpMethod = "GET" } }); // non-existent request url

            // Assert
            Assert.Null(result.Results);
        }

        [Fact]
        public async Task ReturnNullGivenANonExistentHttpVerb()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { 
                    new RequestInfo { 
                        RequestUrl = "/security/alerts/{alert_id}", 
                        HttpMethod = "Foobar" } }); // non-existent http verb

            // Assert
            Assert.Null(result.Results);
        }

        [Theory]
        [InlineData(true, 6)]
        [InlineData(false, 10)]
        public async Task ReturnLeastPrivilegePermissionsForSetOfResources(bool leastPrivilegeOnly, int expectedCount)
        {
            // Arrange
            var requests = new List<RequestInfo>()
            {
                new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" },
                new RequestInfo { RequestUrl = "/sites/{site_id}", HttpMethod = "GET" },
                new RequestInfo { RequestUrl = "/me/tasks/lists/delta", HttpMethod = "GET" }          
            };

            // Act
            var result = await _permissionsStore.GetScopesAsync(
                requests: requests, 
                leastPrivilegeOnly: leastPrivilegeOnly);

            // Assert
            Assert.NotNull(result.Results);
            Assert.NotEmpty(result.Results);
            Assert.Equal(expectedCount, result.Results.Count);
        }

        [Fact]
        public async Task ReturnErrorWhenLeastPrivilegePermissionsForSetOfResourcesIsNotAvailable()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/no/least/privileged/permissions", HttpMethod = "DELETE" } },
                scopeType: ScopeType.DelegatedWork,
                leastPrivilegeOnly: true);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Results);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal("No permissions found.", result.Errors.First().Message);
        }

        [Theory]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range()")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range(address={value})")]
        public async Task RemoveFunctionParametersFromRequestUrlsDuringLoadingAndQueryingOfPermissionsFiles(string url)
        {
            // Act
            var result =
                await _permissionsStore.GetScopesAsync(
                    scopeType: ScopeType.DelegatedWork,
                    requests: new List<RequestInfo>() { new RequestInfo { RequestUrl = url, HttpMethod = "GET" } });

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("Files.ReadWrite", item.ScopeName);
                    Assert.Equal("Have full access to your files", item.DisplayName);
                    Assert.Equal("Allows the app to read, create, update, and delete your files.", item.Description);
                    Assert.False(item.IsAdmin);
                });
        }

        [Fact]
        public async Task ReturnScopesForRequestUrlWhoseScopesInformationNotAvailable()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { 
                    new RequestInfo { 
                        RequestUrl = "/lorem/ipsum/{id}", // bogus URL whose scopes info are unavailable
                        HttpMethod = "GET" 
                    } 
               });

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("LoremIpsum.Read", item.ScopeName);
                    Assert.Equal("Consent name unavailable", item.DisplayName);
                    Assert.Equal("Consent description unavailable", item.Description);
                    Assert.False(item.IsAdmin);
                },
                item =>
                {
                    Assert.Equal("LoremIpsum.ReadWrite", item.ScopeName);
                    Assert.Equal("Consent name unavailable", item.DisplayName);
                    Assert.Equal("Consent description unavailable", item.Description);
                    Assert.False(item.IsAdmin);
                });
        }

        [Fact]
        public async Task ReturnLocalizedPermissionsDescriptionsForSupportedLanguage()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                requests: new List<RequestInfo>() { new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" } },
                scopeType: ScopeType.DelegatedWork,
                locale: "es-ES");

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                    Assert.Equal("Lea los eventos de seguridad de su organización.", item.DisplayName);
                    Assert.Equal("Permite que la aplicación lea los eventos de seguridad de su organización en su nombre.", item.Description);
                    Assert.True(item.IsAdmin);
                },
                item =>
                {
                    Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                    Assert.Equal("Lea y actualice los eventos de seguridad de su organización.", item.DisplayName);
                    Assert.Equal("Permite que la aplicación lea los eventos de seguridad de su organización en su nombre. También le permite actualizar propiedades editables en eventos de seguridad.", item.Description);
                    Assert.True(item.IsAdmin);
                });
        }

        [Fact]
        public async Task ReturnsErrorsForEmptyOrNullRequestUrls()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(
                    requests: new List<RequestInfo>() { 
                        new RequestInfo { RequestUrl = "", HttpMethod = "GET" },
                        new RequestInfo { RequestUrl = null, HttpMethod = "GET" } }
                    );
            // Assert
            Assert.Null(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Equal(2, result.Errors.Count);
            Assert.Collection(result.Errors,
                item =>
                {
                    Assert.Equal("", item.RequestUrl);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                },
                item =>
                {
                    Assert.Null(item.RequestUrl);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                });
        }

        [Fact]
        public async Task ReturnsErrorsForNonExistentRequestUrls()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(
                    requests: new List<RequestInfo>() {
                        new RequestInfo { RequestUrl = "/foo/bar", HttpMethod = "GET" } });
            // Assert
            Assert.Null(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Single(result.Errors);
            Assert.Collection(result.Errors,
                item =>
                {
                    Assert.Equal("/foo/bar", item.RequestUrl);
                    Assert.Equal("Permissions information for 'GET /foo/bar' was not found.", item.Message);
                });
        }

        [Fact]
        public async Task ReturnsUniqueListOfPermissionsForPathsWithSharedPermissions()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(requests: new List<RequestInfo>() { 
                    new RequestInfo { RequestUrl = "/accessreviews", HttpMethod = "GET" }, 
                    new RequestInfo { RequestUrl = "/accessreviews/{id}", HttpMethod = "GET" } }, 
                    scopeType: ScopeType.DelegatedWork);

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("AccessReview.Read.All", item.ScopeName);
                },
                item =>
                {
                    Assert.Equal("AccessReview.ReadWrite.All", item.ScopeName);
                },
                item =>
                {
                    Assert.Equal("AccessReview.ReadWrite.Membership", item.ScopeName);
                });
        }

        [Fact]
        public async Task FetchPermissionsDescriptionsFromGithub()
        {
            //Arrange
            string org = "\\Org";
            string branchName = "Branch";

            // Act
            var result = await _permissionsStore.GetScopesAsync(org: org, branchName: branchName);

            // Assert
            Assert.NotEmpty(result.Results);
        }

        [Fact]
        public async Task FetchPermissionsDescriptionsFromGithubGivenARequestUrl()
        {
            // Arrange
            string org = "\\Org";
            string branchName = "Branch";

            // Act
            var result = await _permissionsStore.GetScopesAsync(org: org, branchName: branchName, scopeType: ScopeType.DelegatedWork, requests: new List<RequestInfo>() { new RequestInfo { RequestUrl = "/security/alerts/{alert_id}", HttpMethod = "GET" } });

            // Assert
            Assert.Collection(result.Results,
                item =>
                {
                    Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                    Assert.Equal("Read your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events on your behalf.", item.Description);
                    Assert.True(item.IsAdmin);
                },
                item =>
                {
                    Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                    Assert.Equal("Read and update your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events on your behalf. Also allows you to update editable properties in security events.", item.Description);
                    Assert.True(item.IsAdmin);
                });
        }
    }
}
