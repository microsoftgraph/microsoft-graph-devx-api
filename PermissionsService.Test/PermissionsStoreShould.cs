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
        public async Task GetAllRequiredPermissionScopesGivenAnExistingRequestUrlAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                requests: new List<RequestInfo> { new RequestInfo { RequestUrl = "/security/alerts/{id}", HttpMethod = "GET" } }, leastPrivilegeOnly: false);

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
        public async Task GetRequiredPermissionScopesGivenAnExistingRequestUrlByScopeTypeAsync(ScopeType scopeType)
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/security/alerts/{id}", HttpMethod = "GET" } },
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
                Assert.Empty(result.Results);
            }
        }

        [Fact]
        public async Task GetLeastPrivilegePermissionScopesGivenAnExistingRequestUrlAsync()
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
        [InlineData(ScopeType.DelegatedWork, 497)]
        [InlineData(ScopeType.DelegatedPersonal, 468)]
        [InlineData(ScopeType.Application, 475)]
        public async Task GetAllPermissionScopesGivenNoRequestUrlFilteredByScopeTypeAsync(ScopeType scopeType, int expectedCount)
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(scopeType: scopeType);

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(expectedCount, result.Results.Count);
        }

        [Fact]
        public async Task GetAllPermissionScopesGivenNoRequestUrlAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync();

            // Assert
            Assert.NotEmpty(result.Results);
            Assert.Equal(939, result.Results.Count);
        }

        [Fact]
        public async Task ReturnNullGivenANonExistentRequestUrlAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/foo/bar/{id}", HttpMethod = "GET" } }); // non-existent request url

            // Assert
            Assert.Empty(result.Results);
        }

        [Fact]
        public async Task ReturnNullGivenANonExistentHttpVerbAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> {
                    new RequestInfo {
                        RequestUrl = "/security/alerts/{alert_id}",
                        HttpMethod = "Foobar" } }); // non-existent http verb

            // Assert
            Assert.Empty(result.Results);
        }

        [Theory]
        [InlineData(true, 6)]
        [InlineData(false, 12)]
        public async Task ReturnLeastPrivilegePermissionsForSetOfResourcesAsync(bool leastPrivilegeOnly, int expectedCount)
        {
            // Arrange
            var requests = new List<RequestInfo>()
            {
                new RequestInfo { RequestUrl = "/security/alerts/{id}", HttpMethod = "GET" },
                new RequestInfo { RequestUrl = "/sites/{id}", HttpMethod = "GET" },
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
        public async Task ReturnCorrectLeastPrivilegePermissionsForResourcesThatHaveMatchMoreThanOneTemplateAsync()
        {
            // Arrange
            var request1 = new List<RequestInfo>()
            {
                new RequestInfo { RequestUrl = "/me/tasks/lists/delta", HttpMethod = "GET" }
            };

            var request2 = new List<RequestInfo>()
            {
                new RequestInfo { RequestUrl = "/me/tasks/lists/{id}", HttpMethod = "GET" }
            };

            // Act
            var result1 = await _permissionsStore.GetScopesAsync(requests: request1);
            var result2 = await _permissionsStore.GetScopesAsync(requests: request2);

            // Assert
            Assert.NotNull(result1?.Results);
            Assert.NotNull(result2?.Results);
            Assert.NotEqual(result1.Results.Count, result2.Results.Count);
        }

        [Fact]
        public async Task ReturnErrorWhenLeastPrivilegePermissionsForSetOfResourcesIsNotAvailableAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                new List<RequestInfo> { new RequestInfo { RequestUrl = "/no/least/privileged/permissions", HttpMethod = "DELETE" } },
                scopeType: ScopeType.DelegatedWork,
                leastPrivilegeOnly: true);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Results);
            Assert.NotNull(result.Errors);
            Assert.Single(result.Errors);
            Assert.Equal("Permissions information for 'DELETE /no/least/privileged/permissions' was not found.", result.Errors.First().Message);
        }

        [Theory]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range()")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range(address={value})")]
        public async Task RemoveFunctionParametersFromRequestUrlsDuringLoadingAndQueryingOfPermissionsFilesAsync(string url)
        {
            // Act
            var result =
                await _permissionsStore.GetScopesAsync(
                    scopeType: ScopeType.DelegatedWork,
                    leastPrivilegeOnly: false,
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
        public async Task ReturnLocalizedPermissionsDescriptionsForSupportedLanguageAsync()
        {
            // Act
            var result = await _permissionsStore.GetScopesAsync(
                requests: new List<RequestInfo>() { new RequestInfo { RequestUrl = "/security/alerts/{id}", HttpMethod = "GET" } },
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
        public async Task ReturnsErrorsForEmptyRequestUrlAsync()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(
                    requests: new List<RequestInfo>() {
                        new RequestInfo { RequestUrl = "", HttpMethod = "GET" } });
            // Assert
            Assert.Empty(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Single(result.Errors);
            Assert.Collection(result.Errors,
                item =>
                {
                    Assert.Equal("", item.RequestUrl);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                });
        }

        [Fact]
        public async Task ReturnsErrorsForNullRequestUrlAsync()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(
                    requests: new List<RequestInfo>() {
                        new RequestInfo { RequestUrl = null, HttpMethod = "GET" } });
            // Assert
            Assert.Empty(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Single(result.Errors);
            Assert.Collection(result.Errors,
                item =>
                {
                    Assert.Null(item.RequestUrl);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                });
        }

        [Fact]
        public async Task ReturnsErrorsForNonExistentRequestUrlsAsync()
        {
            // Act
            PermissionResult result =
                await _permissionsStore.GetScopesAsync(
                    requests: new List<RequestInfo>() {
                        new RequestInfo { RequestUrl = "/foo/bar", HttpMethod = "GET" } });
            // Assert
            Assert.Empty(result.Results);
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
        public async Task ReturnsUniqueListOfPermissionsForPathsWithSharedPermissionsAsync()
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
        public async Task FetchPermissionsDescriptionsFromGithubAsync()
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
        public async Task FetchPermissionsDescriptionsFromGithubGivenARequestUrlAsync()
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
