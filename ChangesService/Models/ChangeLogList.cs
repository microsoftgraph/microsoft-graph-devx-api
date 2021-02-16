// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace ChangesService.Models
{
    /// <summary>
    /// A <see cref="ChangeLog"/> list.
    /// </summary>
    public class ChangeLogList : ChangeLogPagination
    {
        private List<ChangeLog> _changeLogs;

        /// <summary>
        /// The list of changelogs.
        /// </summary>
        [JsonProperty(PropertyName = "ChangeLog")]
        public List<ChangeLog> ChangeLogs
        {
            get
            {
                return _changeLogs;
            }
            set
            {
                _changeLogs = value;
                UpdateTotalItems();
                CurrentItems = _changeLogs.Count;
            }
        }

        /// <summary>
        /// The number of changelog items in the current view of the changelog list.
        /// </summary>
        public int CurrentItems { get => _changeLogs?.Count ?? 0; }

        /// <summary>
        /// The total number of changelog items in the changelog list.
        /// </summary>
        public int TotalItems { get; private set; } = 0;

        /// <summary>
        /// The maximum number of items in a page.
        /// </summary>
        public new int? PageLimit
        {
            get
            {
                return base.PageLimit;
            }
            set
            {
                base.PageLimit = value;

                // Update the total pages to reflect
                // new PageLimit count
                UpdateTotalPages();
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ChangeLogList()
        {
            ChangeLogs = new List<ChangeLog>();
        }

        /// <summary>
        /// Updates the TotalPages property.
        /// </summary>
        private void UpdateTotalPages()
        {
            if (TotalItems > 0 && PageLimit.HasValue)
            {
                int extraPage = 0;
                if (TotalItems % PageLimit > 0)
                {
                    extraPage++;
                }

                TotalPages = (TotalItems / PageLimit.Value) + extraPage;
            }
        }

        /// <summary>
        /// Updates the TotalItems property.
        /// </summary>
        private void UpdateTotalItems()
        {
            if (TotalItems == 0 && ChangeLogs.Any())
            {
                // Update once during the lifetime of this instance
                TotalItems = _changeLogs.Count;
            }
        }
    }
}
