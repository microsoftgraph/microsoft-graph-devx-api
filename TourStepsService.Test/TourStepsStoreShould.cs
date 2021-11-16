using System;
using System.Threading.Tasks;
using TourStepsService.Services;
using TourStepsService.Interfaces;
using TourStepsService.Models;
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
        private IConfigurationRoot _githubFileConfiguration;

        public TourStepsStoreShould()
        {
            _fileUtility = new FileUtilityMock();
            _httpClientUtility = new FileUtilityMock();
            _tourStepsCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json"))
               .Build();
            _githubFileConfiguration = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "GithubTestFiles", "appsettings-test.json"))
               .Build();
        }

        [Fact]
        public async Task CorrectlySeedLocaleCachesOfTourStepsWhenMultipleRequestsReceived()
        {
            //Arrange
            _tourStepsStore = new TourStepsStore(_configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            //Act

            //Fetch en-US tourSteps
            TourStepsList englisthTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("en-US");

            //Fetch es-ES tourSteps
            TourStepsList espanolTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("es-ES");

            //Fetch fr-FR tourSteps
            TourStepsList frenchTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("fr-FR");

            /*Asert*/
            //These might change
            Assert.Equal(26, englisthTourStepsList.TourSteps.Count);
            //Assert.Equal(26, espanolTourStepsList.TourSteps.Count);
            //Assert.Equal(26, frenchTourStepsList.TourSteps.Count);
        }

        [Fact]
        public async Task ReturnNullIfTourStepsFileIsEmpty()
        {
            // Arrange
            _tourStepsStore = new TourStepsStore(_configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            // Act - Fetch ja-JP sample queries which is empty
            TourStepsList japaneseTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("ja-JP");

            // Assert
            Assert.Null(japaneseTourStepsList);
        }



        [Fact]
        public async Task FetchTourStepsFromGithub()
        {
            var configuration = new ConfigurationBuilder()
                           .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "GithubTestFiles", "appsettings-test.json"))
                           .Build();
            string org = configuration["BlobStorage:Org"];
            string branchName = configuration["BlobStorage:Branch"];

            _tourStepsStore = new TourStepsStore(configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            /*Act*/

            //Fetch en-US tour steps
            TourStepsList englishTourStepsList = await _tourStepsStore.FetchTourStepsListAsync("en-US", org, branchName);

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
            string org = configuration["BlobStorage:Org"];
            string branchName = configuration["BlobStorage:Branch"];

            _tourStepsStore = new TourStepsStore(configuration, _httpClientUtility, _fileUtility, _tourStepsCache);

            // Act - Fetch ja-JP samples which is empty
            TourStepsList japaneseTourStepsList = await  _tourStepsStore.FetchTourStepsListAsync("ja-JP", org, branchName);

            //Assert
            Assert.NotNull(japaneseTourStepsList);
        }
        
    }
}
