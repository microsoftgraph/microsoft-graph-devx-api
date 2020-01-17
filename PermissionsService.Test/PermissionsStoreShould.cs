// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using FileService.Services;
using GraphExplorerPermissionsService;
using GraphExplorerPermissionsService.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using Xunit;

namespace PermissionsService.Test
{
    public class PermissionsStoreShould
    {
        private IConfigurationRoot _configuration;
        private IFileUtility _fileUtility;

        public PermissionsStoreShould()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(".\\TestFiles\\appsettingstest-valid.json")
                .Build();

            _fileUtility = new DiskFileUtility();
        }

        [Fact]
        public void GetRequiredPermissionScopesGivenAnExistingRequestUrl()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);                      
            
            // Act
            List<ScopeInformation> result = permissionsStore.GetScopes("/security/alerts/{alert_id}");
                        
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

        [Fact]
        public void ReturnNullGivenANonExistentRequestUrl()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            List<ScopeInformation> result = permissionsStore.GetScopes("/foo/bar/{alert_id}"); // non-existent request url

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentHttpVerb()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            List<ScopeInformation> result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "Foobar"); // non-existent http verb

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentScopeType()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            List<ScopeInformation> result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "PATCH", "Foobar"); // non-existent scope type

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnEmptyArrayForEmptyPermissionScopes()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act by requesting scopes for the 'DelegatedPersonal' scope type
            List<ScopeInformation> result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "GET", "DelegatedPersonal");

            // Assert that returned result is empty
            Assert.Empty(result);
        }

        [Fact]
        public void ReturnScopesForRequestUrlsInEitherPermissionFilesProvided()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            /* Act */

            List<ScopeInformation> result1 = permissionsStore.GetScopes("/users/{id}/calendars/{id}", "GET", "DelegatedWork"); // permission in ver1 doc.
            List<ScopeInformation> result2 = permissionsStore.GetScopes("/anonymousipriskevents/{id}", "GET", "DelegatedWork"); // permission in ver2 doc.
            List<ScopeInformation> result3 = permissionsStore.GetScopes("/security/alerts/{id}", "PATCH", "Application"); // permission in ver1 doc.

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
        }

        [Fact]
        public void RemoveParameterParanthesesFromRequestUrlsDuringLoadingOfPermissionsFiles()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            // RequestUrl in permission file: "/workbook/worksheets/{id}/charts/{id}/image(width=640)"
            List<ScopeInformation> result = permissionsStore.GetScopes("/workbook/worksheets/{id}/charts/{id}/image", "GET", "DelegatedWork");

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
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            List<ScopeInformation> result = permissionsStore.GetScopes("/lorem/ipsum/{id}"); // bogus permission whose scopes info are unavailable

            // Assert
            Assert.Collection(result,
                item =>
                {
                    Assert.Equal("LoremIpsum.Read.All", item.ScopeName);
                    Assert.Equal("Undefined", item.DisplayName);
                    Assert.Equal("Undefined", item.Description);
                    Assert.False(item.IsAdmin);
                },
                item =>
                {
                    Assert.Equal("LoremIpsum.ReadWrite.All", item.ScopeName);
                    Assert.Equal("Undefined", item.DisplayName);
                    Assert.Equal("Undefined", item.Description);
                    Assert.False(item.IsAdmin);
                });
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfTablesNotPopulatedDueToIncorrectPermissionsFilePathName()
        {
            /* Arrange */

            IConfigurationRoot configuration = new ConfigurationBuilder()
               .AddJsonFile(".\\TestFiles\\appsettingstest-invalid.json")
               .Build();

            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, configuration);

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => permissionsStore.GetScopes("/security/alerts/{alert_id}"));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionIfTablesNotPopulatedDueToEmptyPermissionsFile()
        {
            /* Arrange */

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddJsonFile(".\\TestFiles\\appsettingstest-empty.json")
                .Build();

            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, configuration);

            // Act and Assert
            Assert.Throws<InvalidOperationException>(() => permissionsStore.GetScopes("/security/alerts/{alert_id}"));
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfGetScopesRequestUrlParameterIsNullOrEmpty()
        {
            /* Arrange */

            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);
            string nullRequestUrl = null;
            string emptyRequestUrl = string.Empty;

            /* Act and Assert */

            Assert.Throws<ArgumentNullException>(() => permissionsStore.GetScopes(nullRequestUrl)); // null requestUrl arg.
            Assert.Throws<ArgumentNullException>(() => permissionsStore.GetScopes(emptyRequestUrl)); // empty requestUrl arg.
        }
    }
}
