// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace FileService.Extensions
{
    /// <summary>
    /// Extension methods for getting supported locale variants.
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Gets the locale name of the supported <see cref="CultureInfo"/> variant.
        /// </summary>
        /// <param name="cultureInfo">The <see cref="CultureInfo"/> from which to retrieve the supported variant from.</param>
        /// <returns>The locale name of the supported <see cref="CultureInfo"/> variant. The default locale is en-US.</returns>
        public static string GetSupportedLocaleVariant(this CultureInfo cultureInfo)
        {
            var langName = cultureInfo?.TwoLetterISOLanguageName ?? "en"; // english is the default

            // Supported localized file variants.
            return langName switch
            {
                "es" => "es-ES",
                "fr" => "fr-FR",
                "de" => "de-DE",
                "ja" => "ja-JP",
                "pt" => "pt-BR",
                "ru" => "ru-RU",
                "zh" => "zh-CN",
                _ => "en-US",
            };
        }

        /// <summary>
        /// Gets the locale name of the supported <see cref="CultureInfo"/> variant.
        /// </summary>
        /// <param name="cultureInfoName">The name of the <see cref="CultureInfo"/> from which to retrieve the supported variant from.</param>
        /// <returns>The locale name of the supported <see cref="CultureInfo"/> variant. The default locale is en-US.</returns>
        public static string GetSupportedLocaleVariant(this string cultureInfoName)
        {
            return new CultureInfo(cultureInfoName).GetSupportedLocaleVariant();
        }
    }
}
