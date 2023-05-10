// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;

namespace GraphWebApi.Common
{
    /// <summary>
    /// Defines a static class that contains helper methods for handling common HTTP request tasks.
    /// </summary>
    internal static class RequestHelper
    {
        /// <summary>
        /// Gets the preferred language from the Accept-Language key in a request header.
        /// </summary>
        /// <param name="request">The incoming HTTP request object.</param>
        /// <returns>The top preferred language, or null, if non is specified.</returns>
        internal static string GetPreferredLocaleLanguage(HttpRequest request)
        {
            string localeLanguage = null;
            string acceptLanguageHeader = request.Headers.FirstOrDefault(x => x.Key == "Accept-Language").Value.ToString();

            if (!string.IsNullOrEmpty(acceptLanguageHeader))
            {
                var languages = acceptLanguageHeader.Split(',')
               .Select(StringWithQualityHeaderValue.Parse)
               .OrderByDescending(s => s.Quality.GetValueOrDefault(1));

                localeLanguage = languages.FirstOrDefault()?.ToString();
                if (localeLanguage != null)
                {
                    var localeSegments = localeLanguage.Split("-");
                    if (localeSegments.Length >= 2)
                    {
                        localeLanguage = $"{localeSegments[0]}-{localeSegments[1].ToUpper(CultureInfo.InvariantCulture)}";
                    }
                }
            }

            return localeLanguage;
        }
    }
}
