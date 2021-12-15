// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using Newtonsoft.Json;
using System;

namespace ChangesService.Models
{
    /// <summary>
    /// Options for slicing changelog data.
    /// </summary>
    public record ChangeLogQueryOptions
    {
        private int? _top;
        private int _skip;

        /// <summary>
        /// The maximum number of items in a page.
        /// </summary>
        [JsonProperty(PropertyName = "top")]
        public int? Top
        {
            get => _top;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(ChangesServiceConstants.ValueNegativeInteger, nameof(Top));
                }
                _top = value;
            }
        }

        /// <summary>
        /// Number of items skipped.
        /// </summary>
        [JsonProperty(PropertyName = "skip")]
        public int Skip
        {
            get => _skip;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(ChangesServiceConstants.ValueZeroNegativeInteger, nameof(Skip));
                }
                _skip = value;
            }
        }
    }
}
