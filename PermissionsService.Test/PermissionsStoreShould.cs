using FileService.Interfaces;
using FileService.Services;
using GraphExplorerPermissionsService;
using Microsoft.Extensions.Configuration;
using System;
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
            string[] result = permissionsStore.GetScopes("/security/alerts/{alert_id}");

            // Assert
            Assert.Collection(result,
                item =>
                {
                    item.Equals("SecurityEvents.Read.All");
                },
                item =>
                {
                    item.Equals("SecurityEvents.ReadWrite.All");
                });
        }

        [Fact]
        public void ReturnNullGivenANonExistentRequestUrl()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            string[] result = permissionsStore.GetScopes("/foo/bar/{alert_id}"); // non-existent request url

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentHttpVerb()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            string[] result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "Foobar"); // non-existent http verb

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnNullGivenANonExistentScopeType()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            string[] result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "PATCH", "Foobar"); // non-existent scope type

            // Assert that returned result is null
            Assert.Null(result);
        }

        [Fact]
        public void ReturnEmptyArrayForEmptyPermissionScopes()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act by requesting scopes for the 'DelegatedPersonal' scope type
            string[] result = permissionsStore.GetScopes("/security/alerts/{alert_id}", "GET", "DelegatedPersonal");

            // Assert that returned result is null
            Assert.Empty(result);
        }

        [Fact]
        public void ReturnScopesForRequestUrlsInEitherPermissionFilesProvided()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            /* Act */

            string[] result1 = permissionsStore.GetScopes("/users/{id}/calendars/{id}", "GET", "DelegatedWork"); // permission in ver1 doc.
            string[] result2 = permissionsStore.GetScopes("/anonymousipriskevents/{id}", "GET", "DelegatedWork"); // permission in ver2 doc.

            /* Assert */

            Assert.Collection(result1,
                item =>
                {
                    item.Equals("Calendars.Read");
                });

            Assert.Collection(result2,
              item =>
              {
                  item.Equals("IdentityRiskEvent.Read.All");
              });
        }

        [Fact]
        public void RemoveParameterParanthesesFromRequestUrlsDuringLoadingOfPermissionsFiles()
        {
            // Arrange
            PermissionsStore permissionsStore = new PermissionsStore(_fileUtility, _configuration);

            // Act
            // RequestUrl in permission file: "/workbook/worksheets/{id}/charts/{id}/image(width=640)"
            string[] result = permissionsStore.GetScopes("/workbook/worksheets/{id}/charts/{id}/image", "GET", "DelegatedWork");

            /* Assert */

            Assert.Collection(result,
                item =>
                {
                    item.Equals("Files.ReadWrite");
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
