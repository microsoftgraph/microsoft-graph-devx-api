// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Xunit;

namespace UtilityService.Test
{
    public class UtilityFunctionsShould
    {
        [Theory]
        [InlineData("", true)]
        [InlineData(null, true)]
        [InlineData("hello", true)]
        [InlineData("hello!", false)]
        [InlineData("file://C:/hello", false)]
        [InlineData("hello?", false)]
        [InlineData("hello%20", false)]
        public void CheckWhetherInputIsUrlSafe(string input, bool expected)
        {
            // Arrange and Act
            var actual = UtilityFunctions.IsUrlSafe(input);

            // Assert
            Assert.Equal(expected, actual);
        }
    }
}
