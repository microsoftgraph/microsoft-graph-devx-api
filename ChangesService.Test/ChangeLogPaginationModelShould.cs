// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangeLogPaginationModelShould
    {
        readonly ChangeLogQueryOptions _pagination = new ChangeLogQueryOptions();

        [Fact]
        public void ThrowArgumentExceptionIfSkipValueIsNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _pagination.Skip = -1);
        }

        [Fact]
        public void ThrowArgumentExceptionIfTopValueIsZeroOrNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _pagination.Top = 0);
            Assert.Throws<ArgumentException>(() => _pagination.Top = -1);
        }
    }
}
