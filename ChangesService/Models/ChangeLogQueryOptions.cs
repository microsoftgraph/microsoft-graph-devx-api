﻿// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Common;
using System;

namespace ChangesService.Models
{
    /// <summary>
    /// Options for slicing changelog data.
    /// </summary>
    public record ChangeLogQueryOptions
    {
        private const int MaxTopValue = 500;
        private int _top = MaxTopValue;
        private int _skip;

        /// <summary>
        /// The maximum number of items in a page.
        /// </summary>
        public int Top
        {
            get => _top;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(ChangesServiceConstants.ValueNegativeInteger, nameof(Top));
                }

                _top = value > MaxTopValue ? MaxTopValue : value;
            }
        }

        /// <summary>
        /// Number of items skipped.
        /// </summary>
        public int Skip
        {
            get => _skip;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(ChangesServiceConstants.ValueNegativeInteger, nameof(Skip));
                }

                _skip = value;
            }
        }
    }
}
