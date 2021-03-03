// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Initializes a telemetry processor to sanitize http request urls to remove sensitive data.
/// </summary>
public class TelemetryHelper : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly TelemetryClient _telemetryClient;

    private static Regex _guidRegex = new Regex(
        @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public TelemetryHelper(ITelemetryProcessor next, TelemetryClient telemetryClient)
    {
        _next = next;
        _telemetryClient = telemetryClient;
    }

    /// <summary>
    /// Gets a request telemetry item, checks the request name property for any guids and replaces them.
    /// </summary>
    /// <param name="item">A telemetry item.</param>
    public void Process(ITelemetry item)
    {
        var request = item as RequestTelemetry;
        if (request != null)
        {
            // Regex-replace anything that looks like a GUID
            request.Name = _guidRegex.Replace(request.Name, "*****");
            request.Url = new Uri(_guidRegex.Replace(request.Url.ToString(), "*****"));
        }

        _next.Process(item);
    }

    public void BeginRequest(HttpContext context)
    {
        // start a stopwatch to process the request
        var stopWatch = Stopwatch.StartNew();
        context.Items["request-tracking-watch"] = stopWatch;
    }

    public void EndRequest(HttpContext context)
    {
        var stopWatch = (Stopwatch)context.Items["request-tracking-watch"];
        stopWatch.Stop();

        RequestTelemetry requestTelemetry = new RequestTelemetry(
            name: context.Request.Method + " " + context.Request.Path.Value,
            startTime: DateTimeOffset.Now,
            duration: stopWatch.Elapsed,
            responseCode: context.Response.StatusCode.ToString(),
            success: 200 == context.Response.StatusCode
        );
        requestTelemetry.Url = context.Request.GetUri();
        //requestTelemetry.HttpMethod = context.Request.Htt;

        _telemetryClient.TrackRequest(requestTelemetry);
    }
}