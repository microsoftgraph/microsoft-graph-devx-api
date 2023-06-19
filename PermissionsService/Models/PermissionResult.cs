// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Newtonsoft.Json;

namespace PermissionsService.Models
{
    public class PermissionResult
    {
        public List<ScopeInformation> Results
        {
            get; set;
        } = new();

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<PermissionError> Errors
        {
            get; set;
        }
    }

    public class PermissionError
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string RequestUrl
        {
            get; set;
        }

        public string Message
        {
            get; set;
        } = string.Empty;
    }
}
