namespace GraphWebApi.Models
// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

{
    internal static class Constants
    {
        internal static class ClaimTypes
        {
            // User Principal Name
            public const string UpnJwt = "preferred_username";
            public const string UpnUriSchema = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";            
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
