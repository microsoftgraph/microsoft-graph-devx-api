// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using GraphExplorerPermissionsService.Interfaces;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using UriMatchingService;
using UtilityService;

namespace TelemetryService

{
    /// <summary>
    /// Initializes a telemetry processor to sanitize telemetry data to remove sensitive data (PII).
    /// </summary>
    public class CustomPIIFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly UriTemplateMatcher _uriTemplateMatcher;

        private static readonly Regex _guidRegex = new(@"\b[A-F0-9]{8}(?:-[A-F0-9]{4}){3}-[A-F0-9]{12}\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches patterns like users('MeganB@M365x214355.onmicrosoft.com')
        private static readonly Regex _emailRegex = new(@"([a-zA-Z0-9_\.-]+)@([\da-zA-Z\.-]+)\.([a-zA-Z\.]{2,6})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _mobilePhoneRegex = new(@"^(\+\d{1,2}\s?)?1?\-?\.?\s?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex _numberRegex = new(@"[0-9]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly List<Regex> _piiRegexes = new()
        {
            _guidRegex,
            _emailRegex,
            _mobilePhoneRegex,
            _numberRegex
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
            "substringof",
            "contains",
            "indexof",
            "substring"
        };

        private const string RequestPath = "RequestPath";
        private const string RenderedMessage = "RenderedMessage";
        private const string ODataSearchOperator = "$search=";
        private const string ODataFilterOperator = "$filter";
        private const string SecurityMask = "****";

        public CustomPIIFilter(ITelemetryProcessor next, IPermissionsStore permissionsStore)
        {
            _next = next
                ?? throw new ArgumentNullException(nameof(next), $"{ next }: { nameof(next) }");

            if (permissionsStore == null)
            {
                throw new ArgumentNullException(nameof(permissionsStore), $"{ permissionsStore }: { nameof(permissionsStore) }");
            }

            _uriTemplateMatcher = permissionsStore.GetUriTemplateMatcher();
        }

        /// <summary>
        /// Filters telemetry data and calls the next ITelemetryProcessor in the chain.
        /// </summary>
        /// <param name="item">A telemetry Item.</param>
        public void Process(ITelemetry item)
        {
            if (item is EventTelemetry customEvent &&
                customEvent.Properties.ContainsKey(RequestPath) &&
                customEvent.Properties.ContainsKey(RenderedMessage))
            {
                SanitizeTelemetry(customEvent: customEvent);
            }

            if (item is RequestTelemetry request)
            {
                SanitizeTelemetry(request: request);
            }

            if (item is TraceTelemetry trace)
            {
                /* Count properties contain custom
                 * numerical telemetry not required to
                 * be sanitized.
                 */
                if (!trace.Properties.ContainsKey("Count"))
                {
                    SanitizeTelemetry(trace: trace);
                }
            }

            _next.Process(item);
        }

        /// <summary>
        /// Sanitizes telemetry data for any PII.
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

                requestPathValue = SanitizeUrlQueryPath(requestPathValue);
                customEvent.Properties[RequestPath] = requestPathValue;
                customEvent.Properties[RenderedMessage] = customEvent.Properties[RenderedMessage]
                    .Remove(pathValueIndex, pathValueLength)
                    .Insert(pathValueIndex, requestPathValue);
            }

            if (request != null)
            {
                var requestUrl = request.Url.ToString();
                request.Url = new Uri(SanitizeUrlQueryPath(requestUrl), UriKind.RelativeOrAbsolute);
            }

            if (trace != null)
            {
                trace.Message = SanitizeContent(trace?.Message);
            }
        }

        /// <summary>
        /// Sanitizes any PII present in the query path of a url string.
        /// </summary>
        /// <remarks>In the context of DevX API requests,
        /// only the query path requires sanitization.</remarks>
        /// <param name="url">The target url string. Can be of relative or absolute uri kind.</param>
        /// <returns>The string url with PII sanitized from its query path.</returns>
        private string SanitizeUrlQueryPath(string url)
        {
            const char QueryValSeparator = '&';
            var queryPath = url?.Query();

            if (string.IsNullOrEmpty(queryPath))
            {
                return url;
            }

            var queryValues = queryPath.Split(QueryValSeparator, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < queryValues.Length; i++)
            {
                var queryValue = queryValues[i];

                if (queryValue.Contains("url=", StringComparison.OrdinalIgnoreCase))
                {
                    /* Sanitizing only values for below query params:
                        /permissions?requestUrl=xyz or
                        /openapi?url=xyz
                       We expect that the above query param values contain Graph urls.
                       It will be unfeasible sanitizing values from all query params.
                       Some legitimate examples of query params that might be affected include '&openApiVersion=2&graphVersion=v1.0'
                    */

                    var valueIndex = queryValue.IndexOf('=') + 1;
                    var valueSegment = queryValue[valueIndex..];
                    valueSegment = valueSegment.BaseUriPath()
                                               .UriTemplatePathFormat(true);
                    var resultMatch = _uriTemplateMatcher?.Match(new Uri(valueSegment.ToLowerInvariant(), UriKind.RelativeOrAbsolute));

                    if (resultMatch != null)
                    {
                        queryValue = queryValue[0..valueIndex] + resultMatch.Template;
                    }
                    else
                    {
                        queryValue = queryValue[0..valueIndex] + valueSegment;
                        queryValue = SanitizeContent(queryValue);
                    }

                    queryValues[i] = queryValue;
                    break;
                }
            }

            queryPath = string.Join(QueryValSeparator, queryValues);
            return $"{url.BaseUriPath()}?{queryPath}";
        }

        /// <summary>
        /// Sanitizes any PII present in a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all PII sanitized.</returns>
        private string SanitizeContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            var sanitizedContent = HttpUtility.UrlDecode(content);

            foreach (var piiRegex in _piiRegexes)
            {
                if (piiRegex.IsMatch(sanitizedContent))
                {
                    sanitizedContent = piiRegex.Replace(sanitizedContent, SecurityMask);
                }
            }

            return SanitizeODataQueryOptions(sanitizedContent);
        }

        /// <summary>
        /// Sanitizes any PII present in an OData query option of a string content.
        /// </summary>
        /// <param name="content">The target string content.</param>
        /// <returns>The string content with all PII in the query option sanitized.</returns>
        private static string SanitizeODataQueryOptions(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            string sanitizedContent = content;

            if (sanitizedContent.Contains(ODataFilterOperator))
            {
                sanitizedContent = RedactFilterableValues(content);
            }

            if (sanitizedContent.Contains(ODataSearchOperator))
            {
                sanitizedContent = RedactSearchableValues(sanitizedContent);
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
            if (!(content?.Contains(ODataFilterOperator) ?? false))
            {
                return content;
            }

            var contents = content.Split(ODataFilterOperator);

            // e.g. "openapi?url=/users?$filter=displayName eq 'John Doe'&method=GET" --> =displayName eq 'John Doe'&method=GET
            var filterableContent = contents[1];

            foreach (var option in _odataFilterOptions)
            {
                // Matches 'Milk' in example: /Products?$filter=Name eq 'Milk'
                var regex_1 = new Regex(@$"(?<=\b{option}\s*)('(.*?)')", RegexOptions.IgnoreCase);

                // Matches ('Milk', 'Cheese') in example: /Products?$filter=Name in ('Milk', 'Cheese')
                var regex_2 = new Regex(@$"(?<=\b{option}\s*)(\((.*?)\))", RegexOptions.IgnoreCase);

                if (regex_1.IsMatch(filterableContent))
                {
                    filterableContent = regex_1.Replace(filterableContent, SecurityMask);
                }
                else if (regex_2.IsMatch(filterableContent))
                {
                    filterableContent = regex_2.Replace(filterableContent, SecurityMask);
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
            if (!(content?.Contains(ODataSearchOperator) ?? false))
            {
                return content;
            }

            var contents = content.Split(ODataSearchOperator);

            // e.g /openapi?url=/users?$search='displayName:Meghan' --> 'displayName:Meghan'
            var searchableContent = contents[1];

            searchableContent = searchableContent.Replace(searchableContent, SecurityMask);

            return contents[0] + ODataSearchOperator + searchableContent;
        }
    }
}
