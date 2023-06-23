// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;

namespace PermissionsService.Models
{
    public class RequestInfo
    {
        [JsonProperty(PropertyName = "requestUrl")]
        public string RequestUrl
        {
            get; set;
        }

        [JsonProperty(PropertyName = "method")]
        public string HttpMethod
        {
            get; set;
        }
    }
}
