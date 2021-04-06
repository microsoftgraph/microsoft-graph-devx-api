// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Text.RegularExpressions;

namespace GraphWebApi.Telemetry
{
    /// <summary>
    /// Initializes a telemetry processor to sanitize http request urls to remove sensitive data.
    /// </summary>
    public class TelemetryProcessor : ITelemetryProcessor
    {
        private static Regex _guidRegex = new Regex(@"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches patterns like users('MeganB@M365x214355.onmicrosoft.com')
        private static Regex _emailRegex = new Regex(@"([a-zA-Z0-9_\.-]+)@([\da-zA-Z\.-]+)\.([a-zA-Z\.]{2,6})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ITelemetryProcessor _next;

        public TelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next;
        }

        public void Process(ITelemetry item)
        {
            var request = item as RequestTelemetry;
            if (request != null)
            {
                var builder = new UriBuilder(request.Url);
                request.Url = builder.Uri;

                // Regex-replace anything that looks like a GUID or email
                request.Name = _guidRegex.Replace(request.Name, "{customerID}");

                if (_guidRegex.IsMatch(request.Url.AbsoluteUri))
                {
                    request.Url = new Uri(_guidRegex.Replace(request.Url.ToString(), "{customerID}"));
                }
                else if(_emailRegex.IsMatch(request.Url.AbsoluteUri))
                {
                    request.Url = new Uri(_emailRegex.Replace(request.Url.ToString(), "{customerID}"));
                }
            }

            _next.Process(item);
        }
    }
}