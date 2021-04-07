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
        readonly ChangeLogPagination _pagination = new ChangeLogPagination();

        [Fact]
        public void ThrowArgumentExceptionIfPageValueIsZeroOrNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _pagination.Page = 0);
            Assert.Throws<ArgumentException>(() => _pagination.Page = -1);
        }

        [Fact]
        public void ThrowArgumentExceptionIfPageLimitValueIsZeroOrNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _pagination.PageLimit = 0);
            Assert.Throws<ArgumentException>(() => _pagination.PageLimit = -1);
        }
    }
}
