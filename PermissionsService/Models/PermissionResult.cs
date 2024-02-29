// -------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// -------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PermissionsService.Models
{
    public class PermissionResult
    {
        [JsonPropertyName("results")]
        public List<ScopeInformation> Results
        {
            get; set;
        } = [];

        [JsonPropertyName("errors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<PermissionError> Errors
        {
            get; set;
        }
    }

    public class PermissionError
    {
        [JsonPropertyName("requestUrl"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string RequestUrl
        {
            get; set;
        }

        [JsonPropertyName("message")]
        public string Message
        {
            get; set;
        } = string.Empty;
    }
}
