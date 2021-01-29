// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using GraphExplorerSamplesService.Services;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using MockTestUtility;
using Xunit;

namespace SamplesService.Test
{
    public class SamplesStoreShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IMemoryCache _samplesCache;
        private readonly IFileUtility _fileUtility;
        private ISamplesStore _samplesStore;

        public SamplesStoreShould()
        {
            _fileUtility = new FileUtilityMock();
            _samplesCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(".\\TestFiles\\appsettingstest.json")
                .Build();
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfSampleQueriesWhenMultipleRequestsReceived()
        {
            // Arrange
            _samplesStore = new SamplesStore(_configuration, _samplesCache, _fileUtility);

            /* Act */

            // Fetch en-US sample queries
            SampleQueriesList englishSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("en-US");

            // Fetch es-ES sample queries
            SampleQueriesList espanolSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("es-ES");

            // Fetch fr-FR sample queries
            SampleQueriesList frenchSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("fr-FR");

            /* Assert */

            // en-US
            Assert.Equal(151, englishSampleQueriesList.SampleQueries.Count);
            Assert.Equal("Getting Started", englishSampleQueriesList.SampleQueries[0].Category);
            Assert.Equal("my profile", englishSampleQueriesList.SampleQueries[0].HumanName);

            // es-ES
            Assert.Equal(149, espanolSampleQueriesList.SampleQueries.Count);
            Assert.Equal("Introducción", espanolSampleQueriesList.SampleQueries[0].Category);
            Assert.Equal("mi perfil", espanolSampleQueriesList.SampleQueries[0].HumanName);

            // fr-FR
            Assert.Equal(149, frenchSampleQueriesList.SampleQueries.Count);
            Assert.Equal("Requêtes de base", frenchSampleQueriesList.SampleQueries[0].Category);
            Assert.Equal("mon profil", frenchSampleQueriesList.SampleQueries[0].HumanName);
        }

        [Fact]
        public async Task ReturnNullIfSampleQueryFileIsEmpty()
        {
            // Arrange
            _samplesStore = new SamplesStore(_configuration, _samplesCache, _fileUtility);

            // Act - Fetch ja-JP sample queries which is empty
            SampleQueriesList japaneseSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("ja-JP");

            // Assert
            Assert.Null(japaneseSampleQueriesList);
        }

        [Fact]
        public async Task FetchSamplesFromGithub()
        {
            //Arrange
            var configuration = new ConfigurationBuilder()
                            .AddJsonFile(".\\GithubTestFiles\\appsettings-test.json")
                            .Build();

            string org = configuration["BlobStorage:Org"];
            string branchName = configuration["BlobStorage:Branch"];

            _samplesStore = new SamplesStore(configuration: configuration, httpClientUtility: _fileUtility);

            /* Act */

            // Fetch en-US sample queries
            SampleQueriesList englishSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("en-US", org, branchName);

            // Fetch es-ES sample queries
            SampleQueriesList espanolSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("es-ES", org, branchName);

            /* Assert */

            // en-US
            Assert.NotNull(englishSampleQueriesList);
            Assert.Equal(151, englishSampleQueriesList.SampleQueries.Count);

            // es-ES
            Assert.NotNull(espanolSampleQueriesList);
            Assert.Equal(149, espanolSampleQueriesList.SampleQueries.Count);
        }

        [Fact]
        public async Task ReturnNotNullIfSampleQueriesFileHasEmptyJsonObject()
        {
            //Arrange
            var configuration = new ConfigurationBuilder()
                            .AddJsonFile(".\\GithubTestFiles\\appsettings-test.json")
                            .Build();

            string org = configuration["BlobStorage:Org"];
            string branchName = configuration["BlobStorage:Branch"];

            _samplesStore = new SamplesStore(configuration: configuration, httpClientUtility: _fileUtility);

            // Act - Fetch ja-JP sample queries which is empty
            SampleQueriesList japaneseSampleQueriesList = await _samplesStore.FetchSampleQueriesListAsync("ja-JP", org, branchName);

            // Assert
            Assert.NotNull(japaneseSampleQueriesList);
        }
    }
}
