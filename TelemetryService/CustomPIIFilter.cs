// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace TelemetryService

{
    /// <summary>
    /// Initializes a telemetry processor to sanitize telemetry data to remove sensitive data (PII).
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

        private readonly List<Regex> _piiRegexes = new()
        {
            _guidRegex,
            _emailRegex,
            _mobilePhoneRegex,
            _employeeIdRegex
        };

        private static readonly List<string> _propertyNames = new()
        {
                "displayName",
                "firstName",
                "lastName",
                "givenName",
                "preferredName",
                "surname"
        };

        private static readonly List<string> _odataFilterOptions = new()
        {
            "eq",
            "ne",
            "gt",
            "ge",
            "le",
            "lt",
            "in",
            "has",
            "endswith",
            "startswith",
            "substringof"
        };
        private static readonly List<string> _userKeywords = new() { "users", "people" };
        private const string RequestPath = "RequestPath";
        private const string RenderedMessage = "RenderedMessage";
        private const string ODataSearchOperator = "$search=";


        public CustomPIIFilter(ITelemetryProcessor next)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next), $"{ next }: { nameof(next) }");
        }

        /// <summary>
        /// Filters telemetry data and calls the next ITelemetryProcessor in the chain
        /// </summary>
        /// <param name="item">A telemetry Item.</param>
        public void Process(ITelemetry item)
        {
            if (item is EventTelemetry and EventTelemetry customEvent)
            {
                if (customEvent.Properties.ContainsKey(RequestPath) && customEvent.Properties.ContainsKey(RenderedMessage))
                {
                    SanitizeTelemetry(customEvent: customEvent);
                }
            }

            if (item is RequestTelemetry and RequestTelemetry request)
            {
                SanitizeTelemetry(request: request);
            }

            if (item is TraceTelemetry and TraceTelemetry trace)
            {
                SanitizeTelemetry(trace: trace);
            }

            _next.Process(item);
        }

        /// <summary>
        /// Sanitizes telemetry data for any PII
        /// </summary>
        /// <param name="customEvent">Optional: A custom event telemetry.</param>
        /// <param name="request">Optional: A request telemetry.</param>
        /// <param name="trace">Optional: A trace telemetry.</param>
        public void SanitizeTelemetry(EventTelemetry customEvent = null,
                                      RequestTelemetry request = null,
                                      TraceTelemetry trace = null)
        {
            if (customEvent != null)
            {
                var requestPathValue = customEvent.Properties[RequestPath];
                var pathValueLength = requestPathValue.Length;
                var pathValueIndex = customEvent.Properties[RenderedMessage].IndexOf(requestPathValue);

                requestPathValue = SanitizeContent(requestPathValue);
                customEvent.Properties[RequestPath] = requestPathValue;
                customEvent.Properties[RenderedMessage] = customEvent.Properties[RenderedMessage]
                    .Remove(pathValueIndex, pathValueLength)
                    .Insert(pathValueIndex, requestPathValue);
            }

            if (request != null)
            {
                var requestUrl = request.Url.ToString();
                request.Url = new Uri(SanitizeContent(requestUrl));
            }

            if (trace != null)
            {
                trace.Message = SanitizeContent(trace.Message);
            }
        }

        /// <summary>
        /// Sanitizes any PII present in a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all PII sanitized.</returns>
        private string SanitizeContent(string content)
        {
            var sanitizedContent = content;

            foreach (var keyword in _userKeywords)
            {
                if (sanitizedContent.Contains(keyword))
                {
                    foreach (var piiRegex in _piiRegexes)
                    {
                        if (piiRegex.IsMatch(sanitizedContent))
                        {
                            sanitizedContent = piiRegex.Replace(sanitizedContent, "****");
                        }
                    }

                    sanitizedContent = SanitizeQueryOptions(sanitizedContent);
                }
            }

            return sanitizedContent;
        }

        /// <summary>
        /// Sanitizes any PII present in a query option of a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all PII in the query option sanitized.</returns>
        private static string SanitizeQueryOptions(string content)
        {
            string sanitizedContent = content;

            foreach (var propertyName in _propertyNames)
            {
                if (sanitizedContent.Contains(propertyName))
                {
                    sanitizedContent = RedactFilterableValues(content);
                }

                if (sanitizedContent.Contains(ODataSearchOperator))
                {
                    sanitizedContent = RedactSearchableValues(sanitizedContent);
                }
            }

            return sanitizedContent;
        }

        /// <summary>
        /// Redacts any filterable values present in a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all filterable values redacted.</returns>
        private static string RedactFilterableValues(string content)
        {
            const string ODataFilterOperator = "$filter";

            if (!(bool)(content?.Contains(ODataFilterOperator)))
            {
                return content;
            }

            var decodedContent = HttpUtility.UrlDecode(content);
            var contents = decodedContent.Split(ODataFilterOperator);

            if ((bool)!contents?.Any())
            {
                return content;
            }

            // e.g. "openapi?url=/users?$filter=displayName eq 'John Doe'&method=GET" --> =displayName eq 'John Doe'&method=GET
            var filterableContent = contents[1];

            foreach (var option in _odataFilterOptions)
            {
                var pattern_1 = @$"(?<=\b{option}\s*)('(.*?)')"; // will match ex: /Products?$filter=Name eq 'Milk'
                var pattern_2 = @$"(?<=\b{option}\s*)(\((.*?)\))"; // will match ex: /Products?$filter=Name in ('Milk', 'Cheese')
                var regex_1 = new Regex(pattern_1);
                var regex_2 = new Regex(pattern_2);

                if (regex_1.IsMatch(filterableContent))
                {
                    filterableContent = Regex.Replace(filterableContent, pattern_1, "****");
                }
                else if (regex_2.IsMatch(filterableContent))
                {
                    filterableContent = Regex.Replace(filterableContent, pattern_2, "****");
                }
            }

            return contents[0] + ODataFilterOperator + filterableContent;
        }

        /// <summary>
        /// Redacts any searchable values present in a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all searchable values redacted.</returns>
        private static string RedactSearchableValues(string content)
        {
            var contents = content.Split(ODataSearchOperator);

            if (!(bool)contents?.Any())
            {
                return content;
            }

            // e.g /openapi?url=/users?$search='displayName:Meghan' --> 'displayName:Meghan'
            var searchableContent = contents[1];

            searchableContent = searchableContent.Replace(searchableContent, "'****'");

            return contents[0] + ODataSearchOperator + searchableContent;
        }
    }
}