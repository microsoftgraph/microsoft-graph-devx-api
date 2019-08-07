using GraphExplorerSamplesService;
using System;

namespace GraphExplorerSamplesService.Extensions
{
    /// <summary>
    /// Extension methods for formatting strings.
    /// </summary>
    public static class StringFormatterExtension
    {
        /// <summary>
        /// Formats a JSON string into a document-readable JSON-styled string.
        /// </summary>
        /// <param name="value">The JSON string to be formatted.</param>
        /// <returns>The document-readable JSON-styled string.</returns>
        public static string FormatStringForJsonDocument(this string value)
        {
            return
                value
                .Replace("[{\"", "[{\r\n\t\t\"")
                .Replace("},{", "\r\n\t},\r\n\t{\r\n\t\t")
                .Replace("\",\"", "\",\r\n\t\t\"")
                .Replace("}],", "\r\n\t\t}],\r\n\t\t")
                .Replace(",\"", ",\r\n\t\t\"")
                .Replace("}]}", "\r\n\t}]\r\n}");
        }
    }
}
