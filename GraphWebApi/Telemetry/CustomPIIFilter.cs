// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GraphWebApi.Telemetry
{
    /// <summary>
    /// Initializes a telemetry processor to sanitize http request urls to remove sensitive data.
    /// </summary>
    public class CustomPIIFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;

        private static readonly Regex _guidRegex = new(@"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches patterns like users('MeganB@M365x214355.onmicrosoft.com')
        private static readonly Regex _emailRegex = new(@"([a-zA-Z0-9_\.-]+)@([\da-zA-Z\.-]+)\.([a-zA-Z\.]{2,6})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _usernameRegex = new(@"[A-Za-z][A-Za-z0-9._]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CustomPIIFilter(ITelemetryProcessor next)
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
            var customEvent = item as EventTelemetry;

            if (customEvent != null)
            {
                if (customEvent.Properties.ContainsKey("RequestPath") && customEvent.Properties.ContainsKey("RenderedMessage"))
                {
                    //request.Properties.Keys.["RequestPath"];
                    SanitizeEventTelemetry(customEvent);
                }
            }

            _next.Process(item);
        }

        /// <summary>
        /// Sanitizes a custom event telemetry's request path and the rendered message properties
        /// </summary>
        /// <param name="request"> An event telemetry item.</param>
        public void SanitizeEventTelemetry(EventTelemetry customEvent)
        {
            var piiRegexes = new Dictionary<string, Regex>
            {
                {"guidRegex", _guidRegex},
                {"emailRegex", _emailRegex},
            };

            var requestPath = customEvent.Properties["RequestPath"];
            var renderedMessage = customEvent.Properties["RenderedMessage"];

            foreach (var piiRegex in piiRegexes.Values)
            {
                if (piiRegex.IsMatch(requestPath) && piiRegex.IsMatch(renderedMessage))
                {
                    customEvent.Properties["RequestPath"] = piiRegex.Replace(customEvent.Properties["RequestPath"], "****");
                    customEvent.Properties["RenderedMessage"] = piiRegex.Replace(customEvent.Properties["RenderedMessage"], "****");
                }
            }

            SanitizeQueryString(customEvent);
        }

        /// <summary>
        /// Parses the query string in a request url and sanitizes the username property.
        /// </summary>
        /// <param name="requestUrl"> The url of the Http request.</param>
        /// <returns>A sanitized request url.</returns>
        private void SanitizeQueryString(EventTelemetry customEvent)
        {
            if (customEvent != null)
            {
                var requestPath = customEvent.Properties["RequestPath"];
                var renderedMessage = customEvent.Properties["RenderedMessage"];

                if (requestPath.Contains("filter") && requestPath.Contains("displayname"))
                {
                    var newQueryString = FormatString(requestPath);
                    customEvent.Properties["RequestPath"] = newQueryString;
                }

                if(renderedMessage.Contains("filter") && renderedMessage.Contains("displayname"))
                {
                    // Fetch the request path
                    string[] separators = { "GET","responded"};
                    var text = renderedMessage.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    requestPath = text[1];

                    var newQueryString = FormatString(requestPath);

                    // Append sanitized property name to query string
                    newQueryString = $"{text[0]}GET{newQueryString} responded{text[2]}";

                    customEvent.Properties["RenderedMessage"] = newQueryString;
                }
            }
        }

        /// <summary>
        /// Takes in the request path of a custom event telemetry item, parses and sanitizes it.
        /// </summary>
        /// <param name="requestPath"> The request path of a custom event.</param>
        /// <returns>A sanitized request path string.</returns>
        private string FormatString(string requestPath)
        {
            // fetch query string segment
            string[] separators = { "eq", ")" };
            var queryString = requestPath.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Get the property name and sanitize it
            var propertyName = queryString[1];
            if (_usernameRegex.IsMatch(propertyName))
            {
                propertyName = propertyName.Replace(propertyName, " '****'");
            }

            // Append sanitized property name to query string
            var newQueryString = $"{queryString[0]}eq{propertyName})";

            return newQueryString;
        }
    }
}