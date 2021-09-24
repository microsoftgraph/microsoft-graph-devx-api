// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphWebApi.Middleware;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ExceptionMiddlewareShould
{
    public class ExceptionMiddlewareShould
    {
        [Fact]
        public async Task ExceptionMiddlewareShouldThrowArgumentNullException()
        {
            // Arrange
            var expected = "{\"StatusCode\":400,\"Message\":\"Value cannot be null.\"}";

            static Task mockNextMiddleware(HttpContext HttpContext)
            {
                return Task.FromException(new ArgumentNullException());
            }

            // Create the DefaultHttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Call the middleware
            var exceptionHandlingMiddleware = new ExceptionMiddleware(mockNextMiddleware, null);

            // Act
            await exceptionHandlingMiddleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            var bodyContent = "";
            using (var sr = new StreamReader(httpContext.Response.Body))
            {
                bodyContent = sr.ReadToEnd();
            }

            // Assert
            Assert.Equal(expected, bodyContent);
        }

        [Fact]
        public async Task ExceptionMiddlewareShouldThrowInvalidOperationException()
        {
            // Arrange
            var expected = "{\"StatusCode\":400,\"Message\":\"Operation is not valid due to the current state of the object.\"}";

            static Task mockNextMiddleware(HttpContext HttpContext)
            {
                return Task.FromException(new InvalidOperationException());
            }

            // Create the DefaultHttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Call the middleware
            var exceptionHandlingMiddleware = new ExceptionMiddleware(mockNextMiddleware, null);

            // Act
            await exceptionHandlingMiddleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            var bodyContent = "";
            using (var sr = new StreamReader(httpContext.Response.Body))
            {
                bodyContent = sr.ReadToEnd();
            }

            // Assert
            Assert.Equal(expected, bodyContent);
        }

        [Fact]
        public async Task ExceptionMiddlewareShouldThrowArgumentException()
        {
            // Arrange
            var expected = "{\"StatusCode\":404,\"Message\":\"Value does not fall within the expected range.\"}";

            static Task mockNextMiddleware(HttpContext HttpContext)
            {
                return Task.FromException(new ArgumentException());
            }

            // Create the DefaultHttpContext
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();

            // Call the middleware
            var exceptionHandlingMiddleware = new ExceptionMiddleware(mockNextMiddleware, null);

            // Act
            await exceptionHandlingMiddleware.InvokeAsync(httpContext);

            httpContext.Response.Body.Position = 0;
            var bodyContent = "";
            using (var sr = new StreamReader(httpContext.Response.Body))
            {
                bodyContent = sr.ReadToEnd();
            }

            // Assert
            Assert.Equal(expected, bodyContent);
        }

    }
}
