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
        private readonly ITelemetryProcessor _next;

        private static readonly Regex _guidRegex = new(@"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches patterns like users('MeganB@M365x214355.onmicrosoft.com')
        private static readonly Regex _emailRegex = new(@"([a-zA-Z0-9_\.-]+)@([\da-zA-Z\.-]+)\.([a-zA-Z\.]{2,6})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _usernameRegex = new("(fn|ln|lastname|firstname|displayname)");

        public TelemetryProcessor(ITelemetryProcessor next)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next), $"{ next }: { nameof(next) }");
        }

        /// <summary>
        /// Filters request data and calls the next ITelemetryProcessor in the chain
        /// </summary>
        /// <param name="item"> A telemetry Item.</param>
        public void Process(ITelemetry item)
        {
            try
            {
                if (item is RequestTelemetry request)
                {
                    if(request.Name != null)
                    {
                        // Regex-replace anything that looks like a GUID or email
                        request.Name = _guidRegex.Replace(request.Name, "{customerID}");
                    }
                    
                    request.Url = SanitizeUrl(request.Url);
                }
            }
            finally
            {
                _next.Process(item);
            }
        }

        /// <summary>
        /// Takes in a requestUrl param and returns a sanitized url.
        /// </summary>
        /// <param name="requestUrl"> The request url.</param>
        /// <returns> A sanitized url.</returns>
        public Uri SanitizeUrl(Uri requestUrl)
        {
            if (requestUrl != null)
            {
                if (_guidRegex.IsMatch(requestUrl.AbsoluteUri))
                {
                    requestUrl = new Uri(_guidRegex.Replace(requestUrl.ToString(), "{customerID}"));
                }
                else if (_emailRegex.IsMatch(requestUrl.AbsoluteUri))
                {
                    requestUrl = new Uri(_emailRegex.Replace(requestUrl.ToString(), "{redacted-email}"));
                }
                else if (_username.IsMatch(requestUrl.AbsoluteUri))
                {
                    requestUrl = new Uri(_username.Replace(requestUrl.ToString(), "{username}"));
                }
            }

            return requestUrl;
        }
    }
}