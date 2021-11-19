using System;
using System.Threading.Tasks;
using TourStepsService.Services;
using TourStepsService.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using FileService.Interfaces;
using MockTestUtility;
using MemoryCache.Testing.Moq;
using System.IO;
using Xunit;

namespace TourStepsService.Test
{
    public class TourStepsStoreShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IMemoryCache _tourStepsCache;
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private ITourStepsStore _tourStepsStore;

        public TourStepsStoreShould()
        {
            _fileUtility = new FileUtilityMock();
            _httpClientUtility = new FileUtilityMock();
            _tourStepsCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json"))
               .Build();
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfTourStepsWhenMultipleRequestsReceived()
        {
            // Arrange
            _tourStepsStore = new TourStepsStore(_configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            // Act

            // Fetch en-US tourSteps
            var englisthTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("en-US");

            /*Asert*/
            Assert.Equal(26, englisthTourStepsList.TourSteps.Count);
        }

        [Fact]
        public async Task ReturnNullIfTourStepsFileIsEmpty()
        {
            // Arrange
            _tourStepsStore = new TourStepsStore(_configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            // Act - Fetch ja-JP sample queries which is empty
            var japaneseTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("ja-JP");

            // Assert
            Assert.Null(japaneseTourStepsList);
        }

        [Fact]
        public async Task FetchTourStepsFromGithub()
        {
            var configuration = new ConfigurationBuilder()
                           .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "GithubTestFiles", "appsettings-test.json"))
                           .Build();
            var org = configuration["BlobStorage:Org"];
            var branchName = configuration["BlobStorage:Branch"];

            _tourStepsStore = new TourStepsStore(configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            /*Act*/

            // Fetch en-US tour steps
            var englishTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("en-US", org, branchName);

            /*Assert*/
            Assert.NotNull(englishTourStepsList);
            Assert.Equal(26, englishTourStepsList.TourSteps.Count);
        }

        [Fact]
        public async Task ReturnNullIfTourStepsFileHasEmptyJsonObject()
        {
            var configuration = new ConfigurationBuilder()
                           .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "GithubTestFiles", "appsettings-test.json"))
                           .Build();
            var org = configuration["BlobStorage:Org"];
            var branchName = configuration["BlobStorage:Branch"];

            _tourStepsStore = new TourStepsStore(configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            // Act - Fetch ja-JP samples which is empty
            var japaneseTourStepsList = await  _tourStepsStore.FetchTourStepsListAsync("ja-JP", org, branchName);

            // Assert
            Assert.NotNull(japaneseTourStepsList);
        }
    }
}
