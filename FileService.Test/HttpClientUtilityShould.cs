// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FileService.Services;
using MockTestUtility;
using Xunit;

namespace FileService.Test
{
    public class HttpClientUtilityShould
    {
        private readonly HttpClient _httpClientMock;
        private readonly HttpClientUtility _httpClientUtility;

        public HttpClientUtilityShould()
        {
            _httpClientMock = new HttpClient(new MockHttpMessageHandler());
            _httpClientUtility = new HttpClientUtility(_httpClientMock);
        }

        [Fact]
        public async Task ReturnContentAsString()
        {
            HttpContent httpContent = new StringContent(Constants.HttpContent);
            var uri = "http://api/test";

            HttpRequestMessage message = new HttpRequestMessage
            {
                RequestUri =new Uri(uri),
                Content = httpContent
            };

            var content = await _httpClientUtility.ReadFromDocumentAsync(message);

            Assert.Equal(Constants.HttpContent, content);
        }

        [Fact]
        public void ThrowArgumentNullExceptionIfRequestMessageIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => _httpClientUtility.ReadFromDocumentAsync(null).GetAwaiter().GetResult());
        }
    }
}
