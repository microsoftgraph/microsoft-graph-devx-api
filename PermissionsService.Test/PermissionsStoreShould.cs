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
        private const string DelegatedWork = "DelegatedWork";
        private const string Application = "Application";

        public PermissionsStoreShould()
        {
            _permissionsStore = PermissionStoreFactoryMock.GetPermissionStore(ConfigFilePath);
        }

        [Fact]
        public void GetRequiredPermissionScopesGivenAnExistingRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrl: "/security/alerts/{alert_id}", method: "GET")
                                                            .GetAwaiter().GetResult();

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

        [Theory]
        [InlineData(DelegatedWork, 115)]
        [InlineData(Application, 66)]
        public void GetAllPermissionScopesGivenNoRequestUrl(string scopeType, int expectedCount)
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(scopeType: scopeType).GetAwaiter().GetResult();

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(expectedCount, result.Count);

            if (scopeType == DelegatedWork)
            {
                Assert.True(result.Exists(x => x.ScopeName.Equals("Financials.ReadWrite.All")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("identityriskyuser.read.all")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("Foobar.ReadWrite")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("LoremIpsum.ReadWrite")));
            }
            else if (scopeType == Application)
            {
                Assert.True(result.Exists(x => x.ScopeName.Equals("AdministrativeUnit.ReadWrite.All")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("Notes.ReadWrite.All")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("LoremIpsum.Read.All")));
                Assert.True(result.Exists(x => x.ScopeName.Equals("LoremIpsum.ReadWrite.All")));
            }
        }

        [Fact]
        public void ReturnNullGivenANonExistentRequestUrl()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrl: "/foo/bar/{alert_id}", method: "GET") // non-existent request url
                                                            .GetAwaiter().GetResult();

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentHttpVerb()
        {
            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(requestUrl: "/security/alerts/{alert_id}", method: "Foobar") // non-existent http verb
                                                            .GetAwaiter().GetResult();

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentScopeType()
        {
            // Act
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(scopeType: "Foobar",
                                                requestUrl: "/security/alerts/{alert_id}",
                                                method: "PATCH").GetAwaiter().GetResult(); // non-existent scope type

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnEmptyArrayForEmptyPermissionScopes()
        {
            // Act by requesting scopes for the 'DelegatedPersonal' scope type
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(scopeType: "DelegatedPersonal", requestUrl: "/security/alerts/{alert_id}", method: "GET").GetAwaiter().GetResult();

            // Assert that returned result is empty
            Assert.Empty(result);
        }

        [Fact]
        public void ReturnScopesForRequestUrlsInEitherPermissionFilesProvided()
        {
            /* Act */

            List<ScopeInformation> result1 =
                _permissionsStore.GetScopesAsync(scopeType: DelegatedWork, requestUrl: "/users/{id}/calendars/{id}?$orderby=CreatedDate desc", method: "GET").GetAwaiter().GetResult(); // permission in ver1 doc.
            List<ScopeInformation> result2 =
                _permissionsStore.GetScopesAsync(scopeType: DelegatedWork, requestUrl: "/anonymousipriskevents/{id}", method: "GET").GetAwaiter().GetResult(); // permission in ver2 doc.
            List<ScopeInformation> result3 =
                _permissionsStore.GetScopesAsync(scopeType: Application, requestUrl: "/security/alerts/{id}", method: "PATCH").GetAwaiter().GetResult(); // permission in ver1 doc.
            List<ScopeInformation> result4 =
                _permissionsStore.GetScopesAsync(scopeType: DelegatedWork, requestUrl: "/me/photo/$value", method: "PATCH").GetAwaiter().GetResult(); // permission in ver1 doc.

            /* Assert */

            Assert.Collection(result1,
                item =>
                {
                    Assert.Equal("Calendars.Read", item.ScopeName);
                    Assert.Equal("Read your calendars ", item.DisplayName);
                    Assert.Equal("Allows the app to read events in your calendars. ", item.Description);
                    Assert.False(item.IsAdmin);
                });

            Assert.Collection(result2,
              item =>
              {
                  Assert.Equal("IdentityRiskEvent.Read.All", item.ScopeName);
                  Assert.Equal("Read identity risk event information", item.DisplayName);
                  Assert.Equal("Allows the app to read identity risk event information for all users in your organization on behalf of the signed-in user. ", item.Description);
                  Assert.True(item.IsAdmin);
              });

            Assert.Collection(result3,
              item =>
              {
                  Assert.Equal("SecurityEvents.ReadWrite.All", item.ScopeName);
                  Assert.Equal("Read and update your organization's security events", item.DisplayName);
                  Assert.Equal("Allows the app to read your organization's security events without a signed-in user. Also allows the app to update editable properties in security events.", item.Description);
                  Assert.False(item.IsAdmin);
              });

            Assert.Collection(result4,
              item =>
              {
                  Assert.Equal("User.ReadWrite", item.ScopeName);
                  Assert.Equal("Read and update your profile", item.DisplayName);
                  Assert.Equal("Allows the app to read your profile, and discover your group membership, reports and manager. It also allows the app to update your profile information on your behalf.", item.Description);
                  Assert.False(item.IsAdmin);
              },
              item =>
              {
                  Assert.Equal("User.ReadWrite.All", item.ScopeName);
                  Assert.Equal("Read and write all users' full profiles", item.DisplayName);
                  Assert.Equal("Allows the app to read and write the full set of profile properties, reports, and managers of other users in your organization, on your behalf.", item.Description);
                  Assert.True(item.IsAdmin);
              });
        }

        [Fact]
        public void RemoveParameterParanthesesFromRequestUrlsDuringLoadingOfPermissionsFiles()
        {
            // Act
            // RequestUrl in permission file: "/workbook/worksheets/{id}/charts/{id}/image(width=640)"
            List<ScopeInformation> result =
                _permissionsStore.GetScopesAsync(scopeType: DelegatedWork,
                                                 requestUrl: "/workbook/worksheets/{id}/charts/{id}/image",
                                                 method: "GET").GetAwaiter().GetResult();

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
                _permissionsStore.GetScopesAsync(requestUrl: "/lorem/ipsum/{id}",
                                                method: "GET").GetAwaiter().GetResult(); // bogus permission whose scopes info are unavailable

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
                _permissionsStore.GetScopesAsync(requestUrl: "/security/alerts/{alert_id}",
                                                method: "GET",
                                                locale: "es-ES").GetAwaiter().GetResult();

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
        public void ThrowInvalidOperationExceptionIfTablesNotPopulatedDueToEmptyPermissionsFile()
        {
            /* Act & Assert */

            Assert.Throws<InvalidOperationException>(() => PermissionStoreFactoryMock.GetPermissionStore(Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest-empty.json")));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfMethodIsNullOrEmptyAndRequestUrlHasValue()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _permissionsStore.GetScopesAsync(requestUrl: "/security/alerts/{alert_id}")
                                                                       .GetAwaiter().GetResult());
        }

        [Fact]
        public void FetchPermissionsDescriptionsFromGithub()
        {
            //Arrange
            string org = "\\Org";
            string branchName = "Branch";

            // Act
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(org: org, branchName: branchName).GetAwaiter().GetResult();

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
            List<ScopeInformation> result = _permissionsStore.GetScopesAsync(org: org, branchName: branchName, requestUrl: "/security/alerts/{alert_id}", method: "GET")
                                                            .GetAwaiter().GetResult();

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
