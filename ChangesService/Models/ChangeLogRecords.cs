// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ChangesService.Models
{
    /// <summary>
    /// Holds the <see cref="ChangeLog"/> records.
    /// </summary>
    public record ChangeLogRecords : ChangeLogQueryOptions
    {
        private IEnumerable<ChangeLog> _changeLogs = new List<ChangeLog>();

        /// <summary>
        /// Changelogs entries.
        /// </summary>
        [JsonProperty(PropertyName = "ChangeLog")]
        public IEnumerable<ChangeLog> ChangeLogs
        {
            get => _changeLogs;
            set
            {
                _changeLogs = value;
                UpdateTotalItems();
            }
        }

        /// <summary>
        /// The number of changelog items in the current view of the <see cref="ChangeLogRecords"/>.
        /// </summary>
        public int CurrentItems => _changeLogs?.Count() ?? 0;

        /// <summary>
        /// The total number of changelog items in the <see cref="ChangeLogRecords"/>.
        /// </summary>
        public int TotalItems { get; private set; }

        /// <summary>
        /// Updates the TotalItems property.
        /// </summary>
        private void UpdateTotalItems()
        {
            if (TotalItems == 0 && ChangeLogs.Any())
            {
                // Update once during the lifetime of this instance
                TotalItems = _changeLogs.Count();
            }
        }
    }
}
