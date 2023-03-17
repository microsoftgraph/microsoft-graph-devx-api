// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using MockTestUtility;
using PermissionsService.Interfaces;
using PermissionsService.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
        public void GetAllRequiredPermissionScopesGivenAnExistingRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/security/alerts/{alert_id}" }, method: "GET")
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            Assert.Collection(result,
                item => {
                    Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                    Assert.Equal("Read your organization's security events", item.DisplayName);
                    Assert.Equal("Allows the app to read your organization's security events without a signed-in user.", item.Description);
                    Assert.Equal(ScopeType.Application, item.ScopeType);
                    Assert.False(item.IsAdmin);
                    Assert.True(item.IsLeastPrivilege);
                    Assert.False(item.IsHidden);
                },
                item => {
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
        public void GetRequiredPermissionScopesGivenAnExistingRequestUrlByScopeType(ScopeType scopeType)
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/security/alerts/{alert_id}" }, method: "GET", scopeType: scopeType)
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            if (scopeType == ScopeType.DelegatedWork)
            {
                // Assert
                Assert.Collection(result,
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
                Assert.Collection(result,
                    item => {
                        Assert.Equal("SecurityEvents.Read.All", item.ScopeName);
                        Assert.Equal("Read your organization's security events", item.DisplayName);
                        Assert.Equal("Allows the app to read your organization's security events without a signed-in user.", item.Description);
                        Assert.Equal(ScopeType.Application, item.ScopeType);
                        Assert.False(item.IsAdmin);
                        Assert.True(item.IsLeastPrivilege);
                        Assert.False(item.IsHidden);
                    },
                    item => {
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
                Assert.Empty(result);
            }
        }

        [Fact]
        public void GetLeastPrivilegePermissionScopesGivenAnExistingRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/security/alerts/{alert_id}" }, method: "GET", scopeType: ScopeType.Application, leastPrivilegeOnly: true)
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            Assert.Single(result);
            Assert.Collection(result,
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
        [InlineData(ScopeType.DelegatedWork, 292)]
        [InlineData(ScopeType.DelegatedPersonal,38)]
        [InlineData(ScopeType.Application,279)]
        public void GetAllPermissionScopesGivenNoRequestUrlFilteredByScopeType(ScopeType scopeType, int expectedCount)
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(scopeType: scopeType).GetAwaiter().GetResult().Results;

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(expectedCount, result.Count);
        }

        [Fact]
        public void GetAllPermissionScopesGivenNoRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync().GetAwaiter().GetResult().Results;

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(609, result.Count);
        }

        [Fact]
        public void ReturnNullGivenANonExistentRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/foo/bar/{alert_id}" }, method: "GET") // non-existent request url
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentHttpVerb()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/security/alerts/{alert_id}" }, method: "Foobar") // non-existent http verb
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData(true, 10)]
        [InlineData(false, 12)]
        public void ReturnLeastPrivilegePermissionsForSetOfResources(bool leastPrivilegeOnly, int expectedCount)
        {
            // Arrange
            var requestUrls = new List<string>()
            {
                "/security/alerts/{alert_id}",
                "/sites/{site_id}",
                 "/me/tasks/lists/delta"
            };

            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrls: requestUrls, leastPrivilegeOnly: leastPrivilegeOnly)
                                                           .GetAwaiter().GetResult().Results;

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(expectedCount, result.Count);
        }


        [Theory]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range()")]
        [InlineData("/users/{id}/drive/items/{id}/workbook/worksheets/{id}/range(address={value})")]
        public void RemoveFunctionParametersFromRequestUrlsDuringLoadingAndQueryingOfPermissionsFiles(string url)
        {
            // Act
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(scopeType: ScopeType.DelegatedWork,
                                                 requestUrls: new List<string>() { url },
                                                 method: "GET").GetAwaiter().GetResult().Results;

            /* Assert */
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal("Files.ReadWrite", item.ScopeName);
                    Assert.Equal("Have full access to your files", item.DisplayName);
                    Assert.Equal("Allows the app to read, create, update, and delete your files.", item.Description);
                    Assert.False(item.IsAdmin);
                });
        }

        [Fact]
        public void ReturnScopesForRequestUrlWhoseScopesInformationNotAvailable()
        {
            // Act
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/lorem/ipsum/{id}" },
                                                method: "GET").GetAwaiter().GetResult().Results; // bogus permission whose scopes info are unavailable

            // Assert
            Assert.Collection(result,
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
        public void ReturnLocalizedPermissionsDescriptionsForSupportedLanguage()
        {
            // Act
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/security/alerts/{alert_id}" },
                                                method: "GET",
                                                scopeType: ScopeType.DelegatedWork,
                                                locale: "es-ES").GetAwaiter().GetResult().Results;

            // Assert
            Assert.Collection(result,
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
        public void ReturnsErrorsForEmptyOrNullRequestUrls()
        {
            // Act
            PermissionResult result =
                _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "", null },
                                                method: "GET").GetAwaiter().GetResult();
            // Assert
            Assert.Empty(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Equal(2, result.Errors.Count);
            Assert.Collection(result.Errors, 
                item => 
                {
                    Assert.Equal("", item.Url);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                },
                item =>
                {
                    Assert.Null(item.Url);
                    Assert.Equal("The request URL cannot be null or empty.", item.Message);
                });
        }

        [Fact]
        public void ReturnsErrorsForNonExistentRequestUrls()
        {
            // Act
            PermissionResult result =
                _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/foo/bar" },
                                                method: "GET").GetAwaiter().GetResult();
            // Assert
            Assert.Empty(result.Results);
            Assert.NotEmpty(result.Errors);
            Assert.Single(result.Errors);
            Assert.Collection(result.Errors,
                item =>
                {
                    Assert.Equal("/foo/bar", item.Url);
                    Assert.Equal("Permissions information for /foo/bar were not found.", item.Message);
                });
        }

        [Fact]
        public void ReturnsUniqueListOfPermissionsForPathsWithSharedPermissions()
        {
            // Act
            PermissionResult result =
                _permissionsStore.GetScopesAsync(requestUrls: new List<string>() { "/accessreviews", "/accessreviews/{id}" },
                                                method: "GET", scopeType: ScopeType.DelegatedWork).GetAwaiter().GetResult();

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
        public void FetchPermissionsDescriptionsFromGithub()
        {
            //Arrange
            string org = "\\Org";
            string branchName = "Branch";

            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(org: org, branchName: branchName).GetAwaiter().GetResult().Results;

            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void FetchPermissionsDescriptionsFromGithubGivenARequestUrl()
        {
            // Arrange
            string org = "\\Org";
            string branchName = "Branch";

            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(org: org, branchName: branchName, scopeType: ScopeType.DelegatedWork, requestUrls: new List<string>() { "/security/alerts/{alert_id}" }, method: "GET")
                                                            .GetAwaiter().GetResult().Results;

            // Assert
            Assert.Collection(result,
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
