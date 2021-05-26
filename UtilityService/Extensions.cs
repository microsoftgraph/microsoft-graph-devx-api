// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

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
    }
}
