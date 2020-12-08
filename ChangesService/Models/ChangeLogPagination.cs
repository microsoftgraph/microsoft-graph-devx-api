// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using System;

namespace ChangesService.Models
{
    /// <summary>
    /// Provides options for paging changelog data.
    /// </summary>
    public class ChangeLogPagination
    {
        private int _page = 1;
        private int? _pageLimit = null;

        /// <summary>
        /// The current page.
        /// </summary>
        [JsonProperty(PropertyName = "currentPage")]
        public int Page
        {
            get => _page;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(Page), "Value cannot be zero or a negative integer.");
                }
                _page = value;
            }
        }

        /// <summary>
        /// The total page count.
        /// </summary>
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// The maximum number of items in a page.
        /// </summary>
        public int? PageLimit
        {
            get => _pageLimit;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(PageLimit), "Value cannot be zero or a negative integer.");
                }
                _pageLimit = value;
            }
        }
    }
}
