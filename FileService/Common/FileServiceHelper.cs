// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using FileService.Extensions;

namespace FileService.Common
{
    /// <summary>
    /// Defines a static class that contains helper methods that handle common file operations.
    /// </summary>
    public static class FileServiceHelper
    {
        private const int DefaultRefreshTimeInHours = 24;

        /// <summary>
        /// Retrieves the directory name and file name from a given file path source.
        /// </summary>
        /// <param name="filePathSource">The complete file path.</param>
        /// <returns>A tuple projection of the directory name and file name.</returns>
        public static (string DirectoryName, string FileName) RetrieveFilePathSourceValues(string filePathSource)
        {
            CheckArgumentNullOrEmpty(filePathSource, nameof(filePathSource));

            string directoryName = null;
            string fileName = null;

            if (filePathSource.IndexOfAny(new char[] {Path.DirectorySeparatorChar}) > -1)
            {
                // File path source format --> directoryName\\fileName
                var storageValues = filePathSource.Split(new char[] {Path.DirectorySeparatorChar});
                directoryName = storageValues[0];
                fileName = storageValues[1];
            }

            return (directoryName, fileName);
        }

        /// <summary>
        /// Gets the full path identifier name for a localized file.
        /// </summary>
        /// <param name="containerName">The container holding the desired localized file.</param>
        /// <param name="defaultBlobName">The name of the default file.</param>
        /// <param name="localeCode">The language code of the desired file. If empty or null or unsupported, this default to 'en-US'.</param>
        /// <returns>A string path of the fully qualified file name including the container name prepended to the resolved localized file name.</returns>
        public static string GetLocalizedFilePathSource(string containerName, string defaultBlobName, string localeCode = null)
        {
            CheckArgumentNullOrEmpty(containerName, nameof(containerName));
            CheckArgumentNullOrEmpty(defaultBlobName, nameof(defaultBlobName));

            if (!string.IsNullOrEmpty(localeCode))
            {
                localeCode = localeCode.GetSupportedLocaleVariant();

                if (defaultBlobName.Contains('.') && localeCode != "en-US")
                {
                    /* All localized files have a consistent structure, e.g. sample-queries_fr-FR.json
                       except for 'en-Us' --> sample-queries.json or permissions-v1.0.json */

                    string[] blobNameParts = defaultBlobName.Split('.');
                    defaultBlobName = $"{blobNameParts[0]}_{localeCode}.{blobNameParts[1]}";
                }
            }

            // File path source format --> directoryName\\fileName
            return Path.Combine(containerName, defaultBlobName);
        }

        /// <summary>
        /// Check whether the input string is null or empty.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <param name="parameterName">The input parameter name.</param>
        internal static void CheckArgumentNullOrEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(parameterName, "Value cannot be null or empty.");
            }
        }

        /// <summary>
        /// Gets the cache refresh time from a string value.
        /// This defaults to the default constant value if the conversion fails.
        /// </summary>
        /// <param name="value">"The string value of the cache refresh time."</param>
        /// <returns>"The file cache refresh time."</returns>
        public static int GetFileCacheRefreshTime(string value)
        {
            return int.TryParse(value, out int newTime) ? newTime : DefaultRefreshTimeInHours;
        }
    }
}
