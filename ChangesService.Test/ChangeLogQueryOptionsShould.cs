// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System;
using Xunit;

namespace ChangesService.Test
{
    public class ChangeLogQueryOptionsShould
    {
        readonly ChangeLogQueryOptions _queryOption = new();

        [Fact]
        public void ThrowArgumentExceptionIfSkipValueIsNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _queryOption.Skip = -1);
        }

        [Fact]
        public void ThrowArgumentExceptionIfTopValueIsNegativeInteger()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _queryOption.Top = -1);
        }

        [Fact]
        public void ValidateAndSetDefaultMaximumTopValue()
        {
            // Arrange & Act
            _queryOption.Top = 1000;

            // Assert
            Assert.Equal(500, _queryOption.Top);
        }
    }
}
