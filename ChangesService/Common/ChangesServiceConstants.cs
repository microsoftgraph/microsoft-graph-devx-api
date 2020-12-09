// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace ChangesService.Common
{
    /// <summary>
    /// Constants for the ChangesService library.
    /// </summary>
    public static class ChangesServiceConstants
    {
        // Changelog configuration paths constants
        public static string ChangelogRelativeUrlConfigPath = "BlobStorage:Blobs:ChangeLog:RelativeUrl";
        public static string ChangelogBaseUrlConfigPath = "BlobStorage:Blobs:ChangeLog:BaseUrl";
        public static string ChangelogRefreshTimeConfigPath = "FileCacheRefreshTimeInHours:ChangeLog";

        // Microsoft Graph proxy configuration paths constants
        public static string GraphProxyBaseUrlConfigPath = "GraphProxy:BaseUrl";
        public static string GraphProxyRelativeUrlConfigPath = "GraphProxy:RelativeUrl";
        public static string GraphProxyAuthorization = "GraphProxy:Authorization";

        // Error constants
        public static string ValueNullError = "Value cannot be null.";
        public static string JsonStringNullOrEmpty = "The JSON string to be deserialized cannot be null or empty.";
    }
}
