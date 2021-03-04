// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using Newtonsoft.Json;
using System;

namespace ChangesService.Models
{
    /// <summary>
    /// Options for paging changelog data.
    /// </summary>
    public record ChangeLogPagination
    {
        private int _page = 1;
        private int? _pageLimit;

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
                    throw new ArgumentException(ChangesServiceConstants.ValueZeroNegativeInteger, nameof(Page));
                }
                _page = value;
            }
        }

        /// <summary>
        /// The total page count.
        /// </summary>
        public int TotalPages { get; protected set; } = 1;

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
                    throw new ArgumentException(ChangesServiceConstants.ValueZeroNegativeInteger, nameof(PageLimit));
                }
                _pageLimit = value;
            }
        }
    }
}
