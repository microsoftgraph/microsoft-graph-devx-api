// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesServiceShould
    {
        [Fact]
        public void ThrowArgumentNullExceptionOnDeserializeChangeLogListIfJsonStringParameterIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.ChangesService.DeserializeChangeLogList(nullArgument));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogListIfChangeLogListParameterIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.ChangesService.FilterChangeLogList(null, new ChangeLogSearchOptions(), new MicrosoftGraphProxyConfigs()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogListIfSearchOptionsParameterIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.ChangesService.FilterChangeLogList(new ChangeLogList(), null, new MicrosoftGraphProxyConfigs()));
        }

        [Fact]
        public void ThrowArgumentNullExceptionOnFilterChangeLogListIfGraphProxyConfigsParameterIsNull()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.ChangesService.FilterChangeLogList(new ChangeLogList(), new ChangeLogSearchOptions(), null));
        }

        [Fact]
        public void FilterChangeLogListByWorkload()
        {
            // Arrange
            var changeLogList = new ChangeLogList
            {
                ChangeLogs = ChangeLogListModelShould.GetChangeLogList().ChangeLogs
            };

            var searchOptions = new ChangeLogSearchOptions(workload: "Compliance");
            var graphProxyConfigs = new MicrosoftGraphProxyConfigs();

            // Act
            var changeLog = Services.ChangesService.FilterChangeLogList(changeLogList, searchOptions, graphProxyConfigs);

            // Assert
            Assert.NotNull(changeLog);
        }
    }
}
