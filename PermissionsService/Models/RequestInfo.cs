// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace PermissionsService.Models
{
    public class RequestInfo
    {
        [JsonPropertyName("requestUrl")]
        public string RequestUrl
        {
            get; set;
        }

        [JsonPropertyName("method")]
        public string HttpMethod
        {
            get; set;
        }
    }
}
