// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace GraphWebApi.Common
{
    internal static class Constants
    {
        internal static class ClaimTypes
        {
            // User Principal Name
            public const string UpnJwt = "preferred_username";
            public const string UpnUriSchema = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";            
        }
    }
}
