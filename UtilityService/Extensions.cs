// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace UtilityService
{
    /// <summary>
    /// Provides common utility functions.
    /// </summary>
    public static class Extensions
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

            var regex = new Regex(@"^[^?]+");
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

            var regex = new Regex(@"(?<=\?)(.*)");
            return regex.Match(uri).Value;
        }

        /// <summary>
        /// Removes matching open and close parentheses (including the enclosed content) from a string.
        /// </summary>
        /// <example>
        /// 'microsoft.graph.delta()' resolves to 'microsoft.graph.delta'
        /// </example>
        /// <param name="value">The target string value.</param>
        /// <returns>The string value without the open and close parentheses.</returns>
        public static string RemoveParantheses(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return Regex.Replace(value, @"\(.*?\)", string.Empty);
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
        /// <returns>The uri template path format of a given url string.</returns>
        public static string UriTemplatePathFormat(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            const string GraphNamespace = "microsoft.graph.";
            const char ForwardSlash = '/';

            var segments = value.Split(ForwardSlash);

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                if (segment.Contains(GraphNamespace, StringComparison.OrdinalIgnoreCase))
                {
                    /* Resolve action and functions names
                        Ex. microsoft.graph.delta() or microsoft.graph.remove
                    */

                    var namespaceIndex = segment.IndexOf(GraphNamespace);
                    var namespaceSegment = segment[namespaceIndex..];
                    var operationName = namespaceSegment.Replace(GraphNamespace, string.Empty).RemoveParantheses();
                    segment = namespaceIndex > 0 ? segment[0..namespaceIndex] + operationName : operationName;
                }

                if (segment.Contains('(') || segment.Contains(')'))
                {
                    // key is not segment
                    segment = segment.Replace('(', ForwardSlash)
                                     .Replace(')', ForwardSlash)
                                     .TrimEnd(ForwardSlash);
                }

                segments[i] = segment;
            }

            return string.Join(ForwardSlash, segments);
        }
    }
}
