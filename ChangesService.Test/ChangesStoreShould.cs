// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Interfaces;
using ChangesService.Models;
using ChangesService.Services;
using FileService.Interfaces;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MockTestUtility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesStoreShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IFileUtility _fileUtility;
        private readonly IMemoryCache _changesCache;
        private readonly IChangesStore _changesStore;
        private readonly IChangesService _changesService;

        public ChangesStoreShould()
        {
            _changesService = new Services.ChangesService();
            _httpClientUtility = new FileUtilityMock();
            _fileUtility = new FileUtilityMock();
            _changesCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Join(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json"))
                .Build();
            _changesStore = new ChangesStore(_configuration, _changesCache, _changesService, _httpClientUtility, _fileUtility);
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnConstructorIfArgumentsAreNull()
        {
            /* Act and Assert */

            Assert.Throws<ArgumentNullException>(() => new ChangesStore(null, _changesCache, _changesService, _httpClientUtility)); // null configuration
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, null, _changesService, _httpClientUtility)); // null changesCache
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, _changesCache, null, _httpClientUtility)); // null changesService
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, _changesCache, _changesService, null)); // null httpClientUtility
        }

        [Fact]
        public void CorrectlySeedLocaleCachesOfChangeLogRecordsWhenMultipleRequestsMultipleLocaleReceived()
        {
            /* Arrange & Act */

            // Fetch en-US changelog records
            ChangeLogRecords englishChangeLogRecords = FetchChangeLogRecords(new CultureInfo("en-US"));

            // Fetch es-ES changelog records
            ChangeLogRecords espanolChangeLogRecords = FetchChangeLogRecords(new CultureInfo("es"));

            // Fetch fr-FR changelog records
            ChangeLogRecords frenchChangeLogRecords = FetchChangeLogRecords(new CultureInfo("fr-CA"));

            /* Assert */

            // en-US
            Assert.Equal(525, englishChangeLogRecords.ChangeLogs.Count());
            Assert.Equal("Compliance", englishChangeLogRecords.ChangeLogs.FirstOrDefault().WorkloadArea);

            // es-ES
            Assert.Equal(495, espanolChangeLogRecords.ChangeLogs.Count());
            Assert.Equal("Cumplimiento", espanolChangeLogRecords.ChangeLogs.FirstOrDefault().WorkloadArea);

            // fr-FR
            Assert.Equal(495, frenchChangeLogRecords.ChangeLogs.Count());
            Assert.Equal("Conformité", frenchChangeLogRecords.ChangeLogs.FirstOrDefault().WorkloadArea);
        }

        [Fact]
        public void CorrectlySeedLocaleCachesOfChangeLogRecordsWhenMultipleRequestsSingleLocaleReceived()
        {
            /* Arrange & Act */

            // Fetch en-US changelog records
            ChangeLogRecords englishChangeLogRecords1 = FetchChangeLogRecords(new CultureInfo("en-US"));

            // Fetch es-ES changelog records
            ChangeLogRecords englishChangeLogRecords2 = FetchChangeLogRecords(new CultureInfo("en"));

            // Fetch fr-FR changelog records
            ChangeLogRecords englishChangeLogRecords3 = FetchChangeLogRecords(new CultureInfo("en-us"));

            /* Assert */

            // records 1
            Assert.Equal(525, englishChangeLogRecords1.ChangeLogs.Count());
            Assert.Equal("Compliance", englishChangeLogRecords1.ChangeLogs.FirstOrDefault().WorkloadArea);

            // records 2
            Assert.Equal(525, englishChangeLogRecords2.ChangeLogs.Count());
            Assert.Equal("Compliance", englishChangeLogRecords2.ChangeLogs.FirstOrDefault().WorkloadArea);

            // records 3
            Assert.Equal(525, englishChangeLogRecords3.ChangeLogs.Count());
            Assert.Equal("Compliance", englishChangeLogRecords3.ChangeLogs.FirstOrDefault().WorkloadArea);
        }

        [Theory]
        [InlineData("")]
        [InlineData("en-GB")]
        public void SetDefaultLocaleInFetchChangeLogRecords(string locale)
        {
            /* Arrange & Act */

            // Fetch default changelog records
            ChangeLogRecords englishChangeLogRecords = FetchChangeLogRecords(new CultureInfo(locale));
            ChangeLogRecords englishChangeLogRecords1 = FetchChangeLogRecords(null);

            // Assert - we have the English translation
            Assert.Equal(525, englishChangeLogRecords.ChangeLogs.Count());
            Assert.Equal(525, englishChangeLogRecords1.ChangeLogs.Count());

            Assert.Equal("Compliance", englishChangeLogRecords.ChangeLogs.FirstOrDefault().WorkloadArea);
            Assert.Equal("Compliance", englishChangeLogRecords1.ChangeLogs.FirstOrDefault().WorkloadArea);
        }

        [Fact]
        public async Task<Dictionary<string, string>> GetWorkloadServiceMappingsFile()
        {
            // Arrange & Act
            var workloadServiceMappings = await _changesStore.FetchWorkloadServiceMappingsAsync();

            // Assert
            Assert.NotNull(workloadServiceMappings);
            Assert.Equal(106, workloadServiceMappings.Count);

            return workloadServiceMappings;
        }

        public ChangeLogRecords FetchChangeLogRecords(CultureInfo cultureInfo)
        {
            var changeLogRecords = _changesStore.FetchChangeLogRecordsAsync(cultureInfo).GetAwaiter().GetResult();
            Assert.NotNull(changeLogRecords);
            Assert.NotEmpty(changeLogRecords.ChangeLogs);
            return changeLogRecords;
        }
    }
}
