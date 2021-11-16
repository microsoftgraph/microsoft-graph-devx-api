using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using TourStepsService.Models;

namespace TourStepsService.Test
{
    public class TourStepsModelShould
    {
        #region Target Property Test
        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromTargetValueIfPresent()
        {
            //Arrange
            TourStepsModel tourStepsModel = new TourStepsModel();

            //Act
            tourStepsModel.Target = " .signin-section "; //Leading and trailing white spaces

            //Assert
            Assert.False(tourStepsModel.Target.StartsWith(" "));
            Assert.False(tourStepsModel.Target.EndsWith(" "));
            Assert.Equal(".signin-section", tourStepsModel.Target);
        }

        #endregion

        #region Content Property Test
        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromContentValueIfPresent()
        {
            // Arrange
            TourStepsModel tourStepsModel = new TourStepsModel();

            //Act
            tourStepsModel.Content = " Here is the response ";

            //Assert
            Assert.False(tourStepsModel.Content.StartsWith(" "));
            Assert.False(tourStepsModel.Content.EndsWith(" "));
            Assert.Equal("Here is the response", tourStepsModel.Content);
        }
        #endregion

        #region DocLink Property Test
        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromDocLinkValueIfPresent()
        {
            //Arrange
            TourStepsModel tourStepsModel = new TourStepsModel();

            //Act
            tourStepsModel.DocLink = " https://developer.microsoft.com/en-US/office/dev-program ";

            //Assert
            Assert.False(tourStepsModel.DocLink.StartsWith(" "));
            Assert.False(tourStepsModel.DocLink.EndsWith(" "));
            Assert.Equal("https://developer.microsoft.com/en-US/office/dev-program", tourStepsModel.DocLink);

        }

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsSetToInvalidUriValue()
        {
            // Arrange
            TourStepsModel tourStepsModel = new TourStepsModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() =>
                tourStepsModel.DocLink = "microsot/en-US/office/dev-program");
        }

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsNotSetToAbsoluteUriValue()
        {
            // Arrange
            TourStepsModel tourStepsModel = new TourStepsModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() =>
                tourStepsModel.DocLink = "developer.microsoft.com/en-US/office/dev-program");
        }
        #endregion
    }
}
