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
        public const string ChangelogRelativeUrlConfigPath = "BlobStorage:Blobs:ChangeLog:RelativeUrl";
        public const string ChangelogBaseUrlConfigPath = "BlobStorage:Blobs:ChangeLog:BaseUrl";
        public const string ChangelogRefreshTimeConfigPath = "FileCacheRefreshTimeInHours:ChangeLog";

        // Microsoft Graph proxy configuration paths constants
        public const string GraphProxyBaseUrlConfigPath = "GraphProxy:BaseUrl";
        public const string GraphProxyRelativeUrlConfigPath = "GraphProxy:RelativeUrl";
        public const string GraphProxyAuthorization = "GraphProxy:Authorization";

        // Error constants
        public const string ValueNullError = "Value cannot be null.";
        public const string ValueZeroNegativeInteger = "Value cannot be zero or a negative integer.";
        public const string JsonStringNullOrEmpty = "The JSON string to be deserialized cannot be null or empty.";
    }
}
