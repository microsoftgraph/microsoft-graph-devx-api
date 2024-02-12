// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Text.Json.Serialization;

namespace KnownIssuesService.Models
{
    /// <summary>
    /// A Model that will hold the known issues data for front-end rendering
    /// </summary>
    public record KnownIssue
    {
        /// <summary>
        /// Reference Id for every known issue
        /// </summary>
        [JsonPropertyName(nameof(Id))]
        public int? Id { get; set; }

        /// <summary>
        /// Known Issues Title from AZDO
        /// </summary>
        [JsonPropertyName(nameof(Title))]
        public string Title { get; set; }

        /// <summary>
        /// Known Issues Description from AZDO
        /// </summary>
        [JsonPropertyName(nameof(Description))]
        public string Description { get; set; }

        /// <summary>
        /// Microsoft Graph Workload Area
        /// </summary>
        [JsonPropertyName(nameof(WorkLoadArea))]
        public string WorkLoadArea { get; set; }

        /// <summary>
        /// Describes a possible way to solve the specific known issue
        /// </summary>
        
        [JsonPropertyName(nameof(WorkAround))]
        public string WorkAround { get; set; }

        /// <summary>
        /// A Link to the Known Issues Specific Resource Documentation
        /// </summary>
        [JsonPropertyName(nameof(Link))]
        public string Link { get; set; }

        /// <summary>
        /// Date The Issue was Raised
        /// </summary>
        [JsonPropertyName(nameof(CreatedDateTime))]
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Last Change on the Issue  Work Item
        /// </summary>
        [JsonPropertyName(nameof(LastUpdatedDateTime))]
        public DateTime LastUpdatedDateTime { get; set; }

        /// <summary>
        /// Determines if the Last Update is current compared to the Created date
        /// </summary>
        [JsonPropertyName(nameof(IsDateUpdated))]
        public bool IsDateUpdated => LastUpdatedDateTime > CreatedDateTime;

        /// <summary>
        /// Known Issues Status i.e New, Active,Resolved
        /// </summary>
        [JsonPropertyName(nameof(State))]
        public string State { get; set; }

        /// <summary>
        /// Microsoft Graph SubArea
        /// </summary>
        [JsonPropertyName(nameof(SubArea))]
        public string SubArea
        {
            get; set;
        }

        /// <summary>
        /// Microsoft Graph Issue Visibility
        /// </summary>
        public bool IsPublicIssue
        {
            get; set;
        }
    }
}
