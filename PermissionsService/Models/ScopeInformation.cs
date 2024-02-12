// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace PermissionsService.Models
{
    /// <summary>
    /// Defines a representation of a scope information.
    /// </summary>
    public class ScopeInformation
    {
        [JsonPropertyName("value"), JsonRequired]
        public string ScopeName 
        {
            get; set; 
        } = "Scope name unavailable";

        [JsonPropertyName("scopeType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScopeType? ScopeType 
        { 
            get; set; 
        }

        [JsonPropertyName("consentDisplayName"), JsonRequired]
        public string DisplayName 
        { 
            get; set; 
        } = "Consent name unavailable";

        [JsonPropertyName("consentDescription"), JsonRequired]
        public string Description 
        { 
            get; set; 
        } = "Consent description unavailable";

        [JsonPropertyName("isAdmin"), JsonRequired]
        public bool IsAdmin 
        { 
            get; set;
        }

        [JsonPropertyName("isLeastPrivilege"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsLeastPrivilege
        {
            get; set;
        }

        [JsonPropertyName("isHidden")]
        public bool IsHidden
        {
            get; set;
        } = false;
    }
}
