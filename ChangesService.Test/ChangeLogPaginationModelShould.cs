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
        [Fact]
        public void ThrowArgumentExceptionIfPageValueIsZeroOrNegativeInteger()
        {
            // Arrange
            var pagination = new ChangeLogPagination();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => pagination.Page = 0);
            Assert.Throws<ArgumentException>(() => pagination.Page = -1);
        }

        [Fact]
        public void ThrowArgumentExceptionIfPageLimitValueIsZeroOrNegativeInteger()
        {
            // Arrange
            var pagination = new ChangeLogPagination();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => pagination.PageLimit = 0);
            Assert.Throws<ArgumentException>(() => pagination.PageLimit = -1);
        }
    }
}
