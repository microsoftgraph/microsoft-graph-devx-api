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
            var uri = "http://api/test";
            var testContent = MockHttpConstants.UriContentDictionary[uri];
            HttpContent httpContent = new StringContent(testContent);

            HttpRequestMessage message = new HttpRequestMessage
            {
                RequestUri =new Uri(uri),
                Content = httpContent
            };

            var content = await _httpClientUtility.ReadFromDocumentAsync(message);

            Assert.Equal(testContent, content);
        }

        [Fact]
        public async Task ThrowArgumentNullExceptionIfRequestMessageIsNull()
        {
            // Act and Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _httpClientUtility.ReadFromDocumentAsync(null));
        }
    }
}
