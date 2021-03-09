// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphWebApi.Telemetry.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GraphWebApi.Telemetry
{
    public class TelemetryHelper : ITelemetryHelper
    {
        private readonly TelemetryClient _telemetryClient;

        public TelemetryHelper(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public Stopwatch BeginRequest()
        {
            // start a stopwatch to record processing time for the request
            var stopWatch = Stopwatch.StartNew();
            return stopWatch;
        }

        public void EndRequest(Stopwatch stopWatch, HttpContext httpcontext, string eventName)
        {
            stopWatch.Stop();

            var metrics = new Dictionary<string, double>
                {
                    { "processingTime" ,stopWatch.Elapsed.TotalMilliseconds},
                };

            // "Request {method} {url} => {statusCode}" format
            var name = httpcontext.Request.Method + " " + httpcontext.Request.Path.Value;

            var startTime = DateTimeOffset.Now.ToString();
            var responseCode = httpcontext.Response.StatusCode.ToString();

            // Set up properties
            var properties = new Dictionary<string, string>{
                    {"startTime", startTime },
                    {"requestName", name },
                    {"responseCode", responseCode },
                };

            _telemetryClient.TrackEvent(eventName, properties, metrics);
        }
    }
}
