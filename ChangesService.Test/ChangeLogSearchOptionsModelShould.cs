// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangeLogSearchOptionsModelShould
    {
        [Fact]
        public void ThrowInvalidOperationExceptionWhenBothRequestUrlAndWorkloadAreSpecifed()
        {
            // Arrange & Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                new ChangeLogSearchOptions(requestUrl: "/me/", workload: "Extensions"));
        }

        [Fact]
        public void ThrowInvalidOperationExceptionWhenStartDateAndEndDateAndDaysRangeAreSpecified()
        {
            // Arrange
            var startDate = DateTime.Parse("2020-06-01");
            var endDate = DateTime.Parse("2020-12-31");

            // Act & Assert -> startDate and endDate and daysRange are specified
            Assert.Throws<InvalidOperationException>(() =>
                new ChangeLogSearchOptions(startDate: startDate, endDate: endDate, daysRange: 90));
        }
    }
}
