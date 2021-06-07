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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesStoreShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _changesCache;
        private Services.ChangesService _changesService;
        private IChangesStore _changesStore;

        public ChangesStoreShould()
        {
            _changesService = new Services.ChangesService();
            _httpClientUtility = new FileUtilityMock();
            _changesCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(".\\TestFiles\\appsettingstest.json")
                .Build();
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnConstructorIfArgumentsAreNull()
        {
            /* Act and Assert */

            Assert.Throws<ArgumentNullException>(() => new ChangesStore(null, _changesCache, _changesService, _httpClientUtility)); // null configuration
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, null, _changesService, _httpClientUtility)); // null changesCache
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, _changesCache, null, _httpClientUtility)); // null changesService object
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, _changesCache, _changesService, null)); // null httpClientUtility
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfChangeLogRecordsWhenMultipleRequestsMultipleLocaleReceived()
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _changesService, _httpClientUtility);

            /* Act */

            // Fetch en-US changelog records
            ChangeLogRecords englishChangeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            // Fetch es-ES changelog records
            ChangeLogRecords espanolChangeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("es"));

            // Fetch fr-FR changelog records
            ChangeLogRecords frenchChangeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("fr-CA"));

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
        public async Task CorrectlySeedLocaleCachesOfChangeLogRecordsWhenMultipleRequestsSingleLocaleReceived()
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _changesService, _httpClientUtility);

            /* Act */

            // Fetch en-US changelog records
            ChangeLogRecords englishChangeLogRecords1 = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-US"));

            // Fetch es-ES changelog records
            ChangeLogRecords englishChangeLogRecords2 = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en"));

            // Fetch fr-FR changelog records
            ChangeLogRecords englishChangeLogRecords3 = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo("en-us"));

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
        public async Task SetDefaultLocaleInFetchChangeLogRecords(string locale)
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _changesService, _httpClientUtility);

            /* Act */

            // Fetch default changelog records
            ChangeLogRecords englishChangeLogRecords = await _changesStore.FetchChangeLogRecordsAsync(new CultureInfo(locale));
            ChangeLogRecords englishChangeLogRecords1 = await _changesStore.FetchChangeLogRecordsAsync(null);

            // Assert - we have the English translation
            Assert.Equal(525, englishChangeLogRecords.ChangeLogs.Count());
            Assert.Equal(525, englishChangeLogRecords1.ChangeLogs.Count());

            Assert.Equal("Compliance", englishChangeLogRecords.ChangeLogs.FirstOrDefault().WorkloadArea);
            Assert.Equal("Compliance", englishChangeLogRecords1.ChangeLogs.FirstOrDefault().WorkloadArea);
        }
    }
}
