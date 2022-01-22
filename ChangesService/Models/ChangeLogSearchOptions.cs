// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;

namespace ChangesService.Models
{
    /// <summary>
    /// Options for searching and filtering changelog data.
    /// </summary>
    public record ChangeLogSearchOptions : ChangeLogQueryOptions
    {
        public string RequestUrl { get; }
        public string Service { get; }
        public double DaysRange { get; }
        public DateTime? StartDate { get; }
        public DateTime? EndDate { get; }
        public string GraphVersion { get; set; }

        public ChangeLogSearchOptions(string requestUrl = null, string service = null, double daysRange = 0,
                                      DateTime? startDate = null, DateTime? endDate = null, string graphVersion = null)
        {

            if (!string.IsNullOrEmpty(requestUrl) && !string.IsNullOrEmpty(service))
            {
                throw new InvalidOperationException($"Cannot search by both { nameof(requestUrl)} and {nameof(service) } at the same time.");
            }

            if (startDate != null && endDate != null && daysRange > 0)
            {
                throw new InvalidOperationException($"Cannot filter by { nameof(startDate) }, { nameof(endDate) } " +
                    $"and {nameof(daysRange)} at the same time.");
            }

            RequestUrl = requestUrl;
            Service = service;
            DaysRange = daysRange;
            StartDate = startDate;
            EndDate = endDate;
            GraphVersion = graphVersion;
        }
    }
}
