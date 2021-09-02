// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GraphWebApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TelemetryClient _telemetryClient;

        public ExceptionMiddleware(RequestDelegate next, TelemetryClient telemetryClient)
        {
            _next = next;
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Invokes the next middleware in the pipeline and throw any exceptions caught.
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception exception)
            {
                _telemetryClient?.TrackException(exception);
                await HandleGlobalExceptionAsync(httpContext, exception);
            }
        }

        /// <summary>
        /// Gets the Http context and sets the status code result and exception message caught.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns>A response stream with stack trace details.</returns>
        private static Task HandleGlobalExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception switch
            {
                ArgumentNullException => StatusCodes.Status400BadRequest,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status404NotFound,
                Exception => StatusCodes.Status500InternalServerError
            };

            return context?.Response.WriteAsync(new GlobalErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = exception.Message
            }.ToString());
        }
    }
}
