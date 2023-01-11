// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace UtilityService
{
    /// <summary>
    /// Provides commonly used extension methods.
    /// </summary>
    public static class UtilityExtensions
    {
        /// <summary>
        /// Strips out the query path from a uri string.
        /// </summary>
        /// <example>
        /// '/openapi?url=/me/messages' resolves to '/openapi'
        /// </example>
        /// <param name="uri">The target uri string.</param>
        /// <returns>The uri string without the query path.</returns>
        public static string BaseUriPath(this string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            var regex = new Regex(@"^[^?]+", RegexOptions.None, TimeSpan.FromSeconds(5));
            return regex.Match(uri).Value;
        }

        /// <summary>
        /// Gets the query component from a uri string.
        /// </summary>
        /// <example>
        /// '/openapi?url=/me/messages' resolves to 'url=/me/messages'
        /// </example>
        /// <param name="uri">The target uri string.</param>
        /// <returns>The query component from a uri string without the '?'.</returns>
        public static string Query(this string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return uri;
            }

            var regex = new Regex(@"(?<=\?)(.*)", RegexOptions.None, TimeSpan.FromSeconds(5));
            return regex.Match(uri).Value;
        }

        /// <summary>
        /// Removes matching open and close parentheses (including the enclosed content) from a string.
        /// </summary>
        /// <example>
        /// 'microsoft.graph.delta()' resolves to 'microsoft.graph.delta'
        /// 'microsoft.graph.range(address={address})' resolves to 'microsoft.graph.range'
        /// </example>
        /// <param name="value">The target string value.</param>
        /// <returns>The string value without the open and close parentheses.</returns>
        public static string RemoveParentheses(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(value, @"\(.*?\)", string.Empty, RegexOptions.None, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Resolves a url string to the uri template path format.
        /// </summary>
        /// <example>
        /// education/classes(educationClass-id)schools/microsoft.graph.delta()
        /// resolves to education/classes/educationClass-id/schools/delta
        /// </example>
        /// <remarks>
        /// This  format is used to standardize uri templates - resolve all paths to use keys
        /// as segments and simplify action/function names by removing the namespaces from their names.
        /// </remarks>
        /// <param name="value">The target uri string.</param>
        /// <param name="simplifyNamespace">Whether to simplify any fully qualified 'microsoft.graph' namespace present.</param>
        /// <returns>The uri template path format of a given url string.</returns>
        public static string UriTemplatePathFormat(this string value, bool simplifyNamespace = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            const string GraphNamespace = "microsoft.graph";
            const char ForwardSlash = '/';
            const char OpenParen = '(';
            const char CloseParen = ')';
            Match matchFunction = null;

            var segments = value.Split(ForwardSlash);

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (segment.Contains(GraphNamespace, StringComparison.OrdinalIgnoreCase))
                {
                    if (simplifyNamespace)
                    {
                        /* Resolve action and functions names
                         * ex. microsoft.graph.delta() or microsoft.graph.remove
                         */
                        var namespaceIndex = segment.IndexOf(GraphNamespace);
                        var namespaceSegment = segment[namespaceIndex..];
                        var operationName = namespaceSegment.Replace(GraphNamespace, string.Empty)
                                                            .RemoveParentheses()
                                                            .TrimStart('.');
                        segment = namespaceIndex > 0 ? segment[0..namespaceIndex] + operationName : operationName;
                    }
                    else
                    {
                        // Capture a function --> ex: microsoft.graph.delta()
                        matchFunction = Regex.Match(segment, @$"({GraphNamespace}).*\(.*\)", RegexOptions.None, TimeSpan.FromSeconds(5));
                    }
                }

                if (segment.Contains(OpenParen) || segment.Contains(CloseParen))
                {
                    if (segment.Contains('='))
                    {
                        // Don't remove parentheses of namespace-simplified function parameters
                        // ex: /reports/getemailactivityusercounts(period={value})
                        continue;
                    }

                    /* Resolve any possible parentheses as key instances
                     */
                    if (matchFunction?.Success ?? false)
                    {
                        /* Don't remove the parentheses of a function segment
                         * ex: /drive/list/items(listItem-id)microsoft.graph.getActivitiesByInterval()
                         * --> microsoft.graph.getActivitiesByInterval()
                         */
                        var tempSegment = segment.Replace(matchFunction.Value, string.Empty);
                        segment = tempSegment.TrimStart(OpenParen)
                                             .Replace(OpenParen, ForwardSlash)
                                             .Replace(CloseParen, ForwardSlash)
                                             + matchFunction.Value;
                    }
                    else
                    {
                        // ex: /users(user-id) --> resolved to /users/user-id
                        segment = segment.TrimStart(OpenParen)
                                         .Replace(OpenParen, ForwardSlash)
                                         .Replace(CloseParen, ForwardSlash)
                                         .TrimEnd(ForwardSlash);
                    }
                }

                segments[i] = segment;
            }

            return string.Join(ForwardSlash, segments);
        }

        /// <summary>
        /// Change the input string's line breaks
        /// </summary>
        /// <param name="rawString">The raw input string</param>
        /// <param name="newLine">The new line break.</param>
        /// <returns>The changed string.</returns>
        public static string ChangeLineBreaks(this string rawString, string newLine = "\n")
        {
            rawString = rawString.Trim('\n', '\r');
            rawString = rawString.Replace("\r\n", newLine);
            return rawString;
        }
    }
}
