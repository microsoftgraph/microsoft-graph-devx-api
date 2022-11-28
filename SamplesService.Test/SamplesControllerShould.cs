// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Interfaces;
using SamplesService.Interfaces;
using MemoryCache.Testing.Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MockTestUtility;
using Xunit;
using System.IO;
using System;
using SamplesService.Services;
using GraphWebApi.Controllers;
using Microsoft.ApplicationInsights;

namespace SamplesService.Test
{
    public class SamplesControllerShould
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IMemoryCache _samplesCache;
        private readonly IFileUtility _fileUtility;
        private readonly IHttpClientUtility _httpClientUtility;
        private ISamplesStore _samplesStore;
        private TelemetryClient _telemetryClient;

        public SamplesControllerShould()
        {

            _fileUtility = new FileUtilityMock();
            _httpClientUtility = new FileUtilityMock();
            _samplesCache = Create.MockedMemoryCache();
            _configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "TestFiles", "appsettingstest.json"))
                .Build();
        }

        [Fact]
        public void ReturnExpectedStatusCodes()
        {
            _telemetryClient = MockTelemetryClientProvider.MockTelemetryClient);
            _samplesStore = new SamplesStore(_configuration, _httpClientUtility, _fileUtility, _samplesCache, _telemetryClient);
            var controller = new SamplesController(_samplesStore, _telemetryClient);
            controller.Request.Headers.AcceptLanguage = new("en-US"); // TO-DO: need to fix this
            var response = controller.GetSampleQueriesListAsync(search: null, org: null, branchName: null);
            Assert.NotNull(response);
        }
    }
}
