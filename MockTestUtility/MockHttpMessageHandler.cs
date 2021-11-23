// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MockTestUtility
{
    /// <summary>
    /// Mocks a HttpMessageHandler
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// Mocks the SendAsync method for a HttpMessageHandler that takes in a HttpRequestMessage and returns a HttpResponseMessage.
        /// </summary>
        /// <param name="request">The HttpRequestMessage.</param>
        /// <param name="cancellationToken">A cancellation token for cancelling requests.</param>
        /// <returns>A Task from the HttpResponseMessage result.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            StringContent content;
            if (!MockHttpConstants.UriContentDictionary.TryGetValue(request.RequestUri.OriginalString, out string testContent))
            {
                content = new StringContent(MockHttpConstants.HttpRequestError);
            }
            else
            {
                content = new StringContent(testContent);
            }

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = content
            };
            return await Task.FromResult(responseMessage);
        }
    }
}
