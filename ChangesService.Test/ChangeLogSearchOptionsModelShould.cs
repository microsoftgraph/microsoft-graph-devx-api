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
        public void ThrowInvalidOperationExceptionWhenEitherStartDateOrEndDateAndDaysRangeAreSpecified()
        {
            // Arrange
            var startDate = DateTime.Parse("2020-06-01");
            var endDate = DateTime.Parse("2020-12-31");

            // Act & Assert -> both startDate and endDate specified + daysRange
            Assert.Throws<InvalidOperationException>(() =>
                new ChangeLogSearchOptions(startDate: startDate, endDate: endDate, daysRange: 90));

            // Act & Assert -> just startDate specified + daysRange
            Assert.Throws<InvalidOperationException>(() =>
                new ChangeLogSearchOptions(startDate: startDate, daysRange: 90));

            // Act & Assert -> just endDate specified + daysRange
            Assert.Throws<InvalidOperationException>(() =>
                new ChangeLogSearchOptions(endDate: endDate, daysRange: 90));
        }
    }
}
