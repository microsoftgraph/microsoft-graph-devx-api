// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PermissionsService.Models
{
    /// <summary>
    /// Defines a representation of a scope information.
    /// </summary>
    public class ScopeInformation
    {
        [JsonProperty(Required = Required.Always, PropertyName = "value")]
        public string ScopeName 
        {
            get; set; 
        } = "Scope name unavailable";

        [JsonProperty(PropertyName = "scopeType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScopeType? ScopeType 
        { 
            get; set; 
        }

        [JsonProperty(Required = Required.Always, PropertyName = "consentDisplayName")]
        public string DisplayName 
        { 
            get; set; 
        } = "Consent name unavailable";

        [JsonProperty(Required = Required.Always, PropertyName = "consentDescription")]
        public string Description 
        { 
            get; set; 
        } = "Consent description unavailable";

        [JsonProperty(Required = Required.Always, PropertyName = "isAdmin")]
        public bool IsAdmin 
        { 
            get; set;
        }

        [JsonProperty(PropertyName = "isLeastPrivilege", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsLeastPrivilege
        {
            get; set;
        }

        [JsonProperty(Required = Required.Always, PropertyName = "isHidden")]
        public bool IsHidden
        {
            get; set;
        }
    }
}
