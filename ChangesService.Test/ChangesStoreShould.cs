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
using System.Threading.Tasks;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesStoreShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IHttpClientUtility _httpClientUtility;
        private readonly IMemoryCache _changesCache;
        private IChangesStore _changesStore;

        public ChangesStoreShould()
        {
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

            Assert.Throws<ArgumentNullException>(() => new ChangesStore(null, _changesCache, _httpClientUtility)); // null configuration
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, null, _httpClientUtility)); // null changesCache
            Assert.Throws<ArgumentNullException>(() => new ChangesStore(_configuration, _changesCache, null)); // null httpClientUtility
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfChangeLogListsWhenMultipleRequestsMultipleLocaleReceived()
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _httpClientUtility);

            /* Act */

            // Fetch en-US changelog list
            ChangeLogList englishChangeLogList = await _changesStore.FetchChangeLogListAsync("en-US");

            // Fetch es-ES changelog list
            ChangeLogList espanolChangeLogList = await _changesStore.FetchChangeLogListAsync("es-ES");

            // Fetch fr-FR changelog list
            ChangeLogList frenchChangeLogList = await _changesStore.FetchChangeLogListAsync("fr-FR");

            /* Assert */

            // en-US
            Assert.Equal(525, englishChangeLogList.ChangeLogs.Count);
            Assert.Equal("Compliance", englishChangeLogList.ChangeLogs[0].WorkloadArea);

            // es-ES
            Assert.Equal(495, espanolChangeLogList.ChangeLogs.Count);
            Assert.Equal("Cumplimiento", espanolChangeLogList.ChangeLogs[0].WorkloadArea);

            // fr-FR
            Assert.Equal(495, frenchChangeLogList.ChangeLogs.Count);
            Assert.Equal("Conformité", frenchChangeLogList.ChangeLogs[0].WorkloadArea);
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfChangeLogListsWhenMultipleRequestsSingleLocaleReceived()
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _httpClientUtility);

            /* Act */

            // Fetch en-US changelog list
            ChangeLogList englishChangeLogList1 = await _changesStore.FetchChangeLogListAsync("en-US");

            // Fetch es-ES changelog list
            ChangeLogList englishChangeLogList2 = await _changesStore.FetchChangeLogListAsync("en-US");

            // Fetch fr-FR changelog list
            ChangeLogList englishChangeLogList3 = await _changesStore.FetchChangeLogListAsync("en-US");

            /* Assert */

            // list 1
            Assert.Equal(525, englishChangeLogList1.ChangeLogs.Count);
            Assert.Equal("Compliance", englishChangeLogList1.ChangeLogs[0].WorkloadArea);

            // list 2
            Assert.Equal(525, englishChangeLogList2.ChangeLogs.Count);
            Assert.Equal("Compliance", englishChangeLogList2.ChangeLogs[0].WorkloadArea);

            // list 3
            Assert.Equal(525, englishChangeLogList3.ChangeLogs.Count);
            Assert.Equal("Compliance", englishChangeLogList3.ChangeLogs[0].WorkloadArea);
        }

        [Theory]
        [InlineData("")]
        [InlineData("en-GB")]
        public async Task SetDefaultLocaleInFetchChangeLogList(string locale)
        {
            // Arrange
            _changesStore = new ChangesStore(_configuration, _changesCache, _httpClientUtility);

            /* Act */

            // Fetch default changelog list
            ChangeLogList englishChangeLogList = await _changesStore.FetchChangeLogListAsync(locale);

            // Assert - we have the English translation
            Assert.Equal(525, englishChangeLogList.ChangeLogs.Count);
            Assert.Equal("Compliance", englishChangeLogList.ChangeLogs[0].WorkloadArea);
        }
    }
}
