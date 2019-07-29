using GraphExplorerSamplesService;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SamplesService.Test
{
    public class SampleQueryModelShould
    {
        #region HumanName Property Test
        [Fact]
        public void ThrowArgumentOutOfRangeExceptionIfHumanNamePropertyIsSetToMoreThan64Characters()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => sampleQueryModel.HumanName = @"abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz");
        }
        #endregion

        #region DocLink Property Test
        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsSetToInvalidUriValue()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => sampleQueryModel.DocLink = "microsoft/en-us/graph/docs/api-reference/v1.0/resources/users");
        }

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsNotSetToAbsoluteUriValue()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => sampleQueryModel.DocLink = "developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users");
        }
        #endregion
    }
}
