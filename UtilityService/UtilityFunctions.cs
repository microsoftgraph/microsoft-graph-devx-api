// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;

namespace UtilityService
{
    /// <summary>
    /// Provides commonly used utility functions
    /// </summary>
    public static class UtilityFunctions
    {
        /// <summary>
        /// Check whether the input argument value is null or not.
        /// </summary>
        /// <typeparam name="T">The input value type.</typeparam>
        /// <param name="value">The input value.</param>
        /// <param name="parameterName">The input parameter name.</param>
        /// <returns>The input value.</returns>
        public static T CheckArgumentNull<T>(T value, string parameterName) where T : class
        {
            return value ?? throw new ArgumentNullException(parameterName, $"Value cannot be null: {parameterName}");
        }

        /// <summary>
        /// Check whether the input string value is null or empty.
        /// </summary>
        /// <param name="value">The input string value.</param>
        /// <param name="parameterName">The input parameter name.</param>
        /// <returns>The input value.</returns>
        public static string CheckArgumentNullOrEmpty(string value, string parameterName)
        {
            return string.IsNullOrEmpty(value) ? throw new ArgumentNullException(parameterName, $"Value cannot be null or empty: {parameterName}") : value;
        }

        /// <summary>
        /// Validates whether an input string has URL-safe characters.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <returns>True whether the input string does not contain URL-safe characters.</returns>
        public static bool IsUrlSafe(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            const string pattern = @"[^a-zA-Z0-9\-._~]";
            return !Regex.IsMatch(input, pattern);
        }
    }
}
