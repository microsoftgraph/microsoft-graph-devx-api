// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace OpenAPIService.Common
{
    internal static class Constants
    {
        internal static class GraphConstants
        {
            public static readonly string GraphAuthorizationUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
            public static readonly string GraphTokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
            public static readonly string GraphUrl = "https://graph.microsoft.com/{0}/";
        }

        internal static class OpenApiConstants
        {
            public const string OpenApiVersion_2 = "2";
            public const string OpenApiVersion_3 = "3";
            public const string GraphVersion_V1 = "v1.0";
            public const string GraphVersion_Beta = "beta";
            public const string Format_Yaml = "yaml";
            public const string Format_Json = "json";
        }
    }    
}
