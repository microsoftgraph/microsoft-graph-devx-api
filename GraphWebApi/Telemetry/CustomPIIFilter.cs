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

        private static readonly Regex _mobilePhoneRegex = new(@"^(\+\d{1,2}\s?)?1?\-?\.?\s?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _employeeIdRegex = new(@"[0-9]{7}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<Regex> piiRegexes = new List<Regex>
            {
                _guidRegex,
                _emailRegex,
                _mobilePhoneRegex,
                _employeeIdRegex
            };

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
            if (item is EventTelemetry)
            {
                var customEvent = item as EventTelemetry;
                if (customEvent != null)
                {
                    if (customEvent.Properties.ContainsKey("RequestPath") && customEvent.Properties.ContainsKey("RenderedMessage"))
                    {
                        SanitizeEventTelemetry(customEvent);
                    }
                }
            }
            if(item is RequestTelemetry)
            {
                var request = item as RequestTelemetry;
                if(request != null)
                {
                    SanitizeEventTelemetry(request: request);
                }
            }

            _next.Process(item);
        }

        /// <summary>
        /// Sanitizes a custom event telemetry's request path and the rendered message properties
        /// </summary>
        /// <param name="request"> An event telemetry item.</param>
        public void SanitizeEventTelemetry(EventTelemetry customEvent = null, RequestTelemetry request = null)
        {
            if(customEvent != null)
            {
                var requestPath = customEvent.Properties["RequestPath"];
                var renderedMessage = customEvent.Properties["RenderedMessage"];

                foreach (var piiRegex in piiRegexes)
                {
                    if (piiRegex.IsMatch(requestPath) && piiRegex.IsMatch(renderedMessage))
                    {
                        customEvent.Properties["RequestPath"] = piiRegex.Replace(customEvent.Properties["RequestPath"], "****");
                        customEvent.Properties["RenderedMessage"] = piiRegex.Replace(customEvent.Properties["RenderedMessage"], "****");
                    }
                }
                SanitizeQueryString(customEvent);
            }
            if(request != null)
            {
                var requestUrl = request.Url.ToString();

                foreach (var piiRegex in piiRegexes)
                {
                    if (piiRegex.IsMatch(requestUrl))
                    {
                        request.Url = new Uri(piiRegex.Replace(requestUrl, "****"));
                    }
                }

                SanitizeQueryString(request: request);
            }
        }

        /// <summary>
        /// Parses the query string in a request url and sanitizes the username property.
        /// </summary>
        /// <param name="requestUrl"> The url of the Http request.</param>
        /// <returns>A sanitized request url.</returns>
        private void SanitizeQueryString(EventTelemetry customEvent = null, RequestTelemetry request = null)
        {
            var propertyNames = new List<string>
            {
                "displayName",
                "firstName",
                "lastName",
                "givenName",
                "surname"
            };

            foreach (var propertyName in propertyNames)
            {
                if (customEvent != null)
                {
                    var requestPath = customEvent.Properties["RequestPath"];
                    var renderedMessage = customEvent.Properties["RenderedMessage"];

                    if (requestPath.Contains("filter") && requestPath.Contains(propertyName))
                    {
                        var newQueryString = RedactUserName(requestPath);
                        customEvent.Properties["RequestPath"] = newQueryString;
                    }
                    if (renderedMessage.Contains("filter") && renderedMessage.Contains(propertyName))
                    {
                        // Fetch the request path
                        string[] separators = { "GET", "responded" };
                        var text = renderedMessage.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                        requestPath = text[1];

                        var newQueryString = RedactUserName(requestPath);

                        // Append sanitized property name to query string
                        newQueryString = $"{text[0]}GET{newQueryString}responded{text[2]}";

                        customEvent.Properties["RenderedMessage"] = newQueryString;
                    }
                }
                if (request != null)
                {
                    var requestUrl = request.Url.ToString();

                    if (requestUrl.Contains("filter") && requestUrl.Contains(propertyName))
                    {
                        var newQueryString = RedactUserName(requestUrl);
                        request.Url = new Uri(newQueryString);
                    }
                }
            }
        }

        /// <summary>
        /// Takes in the request path of a custom event telemetry item, parses and sanitizes it.
        /// </summary>
        /// <param name="requestPath"> The request path of a custom event.</param>
        /// <returns>A sanitized request path string.</returns>
        private string RedactUserName(string requestPath)
        {
            var queryString = requestPath.Split("\'");

            // Get the property name and sanitize it
            var propertyName = queryString[1];

            if (_usernameRegex.IsMatch(propertyName))
            {
                propertyName = propertyName.Replace(propertyName, "'****'");
            }

            // Append sanitized property name to query string
            var newQueryString = $"{queryString[0] + propertyName + queryString[2]}";

            return newQueryString;
        }
    }
}