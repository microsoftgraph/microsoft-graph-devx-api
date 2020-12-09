// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;

namespace FileService.Common
{
    /// <summary>
    /// Defines constants for the File Service.
    /// </summary>
    public static class FileServiceConstants
    {
        public const char DirectorySeparator = '\\';

        public static class HttpRequest
        {
            public enum Headers
            {
                Accept,
                Authorization,
                [Description("User-Agent")]
                UserAgent
            }

            public static string ApplicationJsonMediaType = "application/json";
            public static string DevxApiUserAgent = "DevX-API-v1.0";
        }
    }
}
