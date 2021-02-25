// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Text.RegularExpressions;

/// <summary>
/// Initialize a telemetry processor to sanitize http request urls to remove sensitive data
/// </summary>
public class RequestUrlFilter : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    private static Regex _guidRegex = new Regex(
        @"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public RequestUrlFilter(ITelemetryProcessor next)
    {
        _next = next;
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
}