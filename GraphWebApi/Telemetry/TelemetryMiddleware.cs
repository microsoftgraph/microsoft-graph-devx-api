// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace GraphWebApi.Telemetry
{
    /// <summary>
    /// This class implements a telemetry middleware for correlating telemetry operations
    /// </summary>
    public class TelemetryMiddleware
    {
        private readonly RequestDelegate _next;

        public TelemetryMiddleware(RequestDelegate next)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next), $"{ next }: { nameof(next) }");
        }

        public async Task Invoke(HttpContext context, TelemetryClient client)
        {
            if(context != null)
            {
                // Let's create and start RequestTelemetry.
                var requestTelemetry = new RequestTelemetry
                {
                    // "Request {method} {url} => {statusCode}" format
                    Name = $"{context.Request.Method + " " + context.Request.Path.Value}"
                };
                requestTelemetry.Properties.Add("HttpMethod", context.Request.Method);

                var requestHeaders = context.Request.Headers;

                // If there is a Request-Id received from the upstream service, set the telemetry context accordingly.
                if (requestHeaders.ContainsKey("Request-Id"))
                {
                    var requestId = requestHeaders["Request-Id"];

                    // Get the operation ID from the Request-Id
                    requestTelemetry.Context.Operation.Id = GetOperationId(requestId);
                    requestTelemetry.Context.Operation.ParentId = requestId;
                }

                // StartOperation is a helper method that allows correlation of
                // current operations with nested operations/telemetry
                // and initializes start time and duration on telemetry items.
                using var operation = client?.StartOperation(requestTelemetry);
                {
                    // Process the request.
                    try
                    {
                        // Call next middleware in the pipeline
                        await _next(context);
                    }
                    catch (Exception e)
                    {
                        requestTelemetry.Success = false;
                        client.TrackException(e);
                        throw;
                    }
                    finally
                    {
                        // Update status code and success as appropriate.
                        if (context.Response != null)
                        {
                            requestTelemetry.ResponseCode = context.Response.StatusCode.ToString();
                            requestTelemetry.Success = context.Response.StatusCode is >= 200 and <= 299;
                        }
                        else
                        {
                            requestTelemetry.Success = false;
                        }
                        // Stop the operation (and track telemetry)
                        client.StopOperation(operation);
                    }
                }
            }
        }

        private static string GetOperationId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            // Returns the root ID from the '|' to the first '.' if any.
            var rootEnd = id.IndexOf('.');
            if (rootEnd < 0)
                rootEnd = id.Length;

            var rootStart = id[0] == '|' ? 1 : 0;
            return id.Substring(rootStart, rootEnd - rootStart);
        }
    }
}
