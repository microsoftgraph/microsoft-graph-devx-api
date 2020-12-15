// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangesServiceShould
    {
        [Fact]
        public void ThrowArgumentNullExceptionIfDeserializeChangeLogListIfJsonStringParameterIsNull()
        {
            // Arrange
            string nullArgument = "";

            // Act and Assert
            Assert.Throws<ArgumentNullException>(() =>
                Services.ChangesService.DeserializeChangeLogList(nullArgument));
        }
    }
}
