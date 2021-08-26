// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UtilityService;

namespace GraphWebApi.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _traceProperties;

        public ExceptionMiddleware(RequestDelegate next, TelemetryClient telemetryClient)
        {
            _next = next;
            UtilityFunctions.CheckArgumentNull(telemetryClient, nameof(telemetryClient));
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
            catch (ArgumentNullException argNullException)
            {
                _telemetryClient?.TrackException(argNullException, _traceProperties);
                await HandleGlobalExceptionAsync(httpContext, argNullException);
            }
            catch (InvalidOperationException invalidOps)
            {
                _telemetryClient?.TrackException(invalidOps, _traceProperties);
                await HandleGlobalExceptionAsync(httpContext, invalidOps);
            }
            catch (ArgumentException argException)
            {
                _telemetryClient?.TrackException(argException, _traceProperties);
                await HandleGlobalExceptionAsync(httpContext, argException);
            }
            catch (Exception exception)
            {
                _telemetryClient?.TrackException(exception, _traceProperties);
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
                Exception => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError,
            };

            return context?.Response.WriteAsync(new GlobalErrorDetails()
            {
                StatusCode = context.Response.StatusCode,
                Message = exception.Message
            }.ToString());
        }
    }
}
