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
                    if (!string.IsNullOrEmpty(request.Name))
                    {
                        // Regex-replace anything that looks like a GUID
                        request.Name = _guidRegex.Replace(request.Name, "{ID}");
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
        private static Uri SanitizeUrl(Uri requestUrl)
        {
            if (requestUrl != null)
            {
                if (_guidRegex.IsMatch(requestUrl.AbsoluteUri))
                {
                    requestUrl = new Uri(_guidRegex.Replace(requestUrl.ToString(), "{ID}"));
                }
                else if (_emailRegex.IsMatch(requestUrl.AbsoluteUri))
                {
                    requestUrl = new Uri(_emailRegex.Replace(requestUrl.ToString(), "{redacted-email}"));
                }
                else if (_usernameRegex.IsMatch(requestUrl.AbsoluteUri))
                {
                    var sanitizedQueryUrl = ParseQueryString(requestUrl.AbsoluteUri);
                    requestUrl = new Uri(sanitizedQueryUrl);
                }
            }

            return requestUrl;
        }

        /// <summary>
        /// Parses the query string in a request url and sanitizes it.
        /// </summary>
        /// <param name="requestUrl"> The url of the Http request.</param>
        /// <returns>A sanitized request url.</returns>
        private static string ParseQueryString(string requestUrl)
        {
            if (requestUrl != null)
            {
                if (requestUrl.Contains("filter") && requestUrl.Contains("displayname"))
                {
                    // fetch query string segment
                    var queryString = requestUrl.Split("$filter");

                    string[] separators = { "(", "eq", ")" };
                    string[] textInBrackets = queryString[1].Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    // Get the property name and sanitize it
                    var propertyName = textInBrackets[1];
                    if (_usernameRegex.IsMatch(queryString[1]))
                    {
                        propertyName = propertyName.Replace(propertyName, " '{username}'");
                    }

                    // Append sanitized property name to query string
                    var newQueryString = "$filter(" + textInBrackets[0] + "eq" + propertyName;

                    // Modify requestUrl by appending newQueryString to request url
                    requestUrl = queryString[0] + newQueryString;
                }
            }

            return requestUrl;
        }
    }
}