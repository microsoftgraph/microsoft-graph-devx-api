// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
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
            // Arrange
            var tourStepsModel = new TourStepsModel();

            // Act
            tourStepsModel.Target = " .signin-section "; // Leading and trailing white spaces

            // Assert
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
            var tourStepsModel = new TourStepsModel();

            //Act
            tourStepsModel.Content = " Here is the response ";

            // Assert
            Assert.False(tourStepsModel.Content.StartsWith(" "));
            Assert.False(tourStepsModel.Content.EndsWith(" "));
            Assert.Equal("Here is the response", tourStepsModel.Content);
        }
        #endregion

        #region DocLink Property Test
        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromDocLinkValueIfPresent()
        {
            // Arrange
            var tourStepsModel = new TourStepsModel();

            // Act
            tourStepsModel.DocsLink = " https://developer.microsoft.com/en-US/office/dev-program ";

            // Assert
            Assert.False(tourStepsModel.DocsLink.StartsWith(" "));
            Assert.False(tourStepsModel.DocsLink.EndsWith(" "));
            Assert.Equal("https://developer.microsoft.com/en-US/office/dev-program", tourStepsModel.DocsLink);

        }

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsSetToInvalidUriValue()
        {
            // Arrange
            var tourStepsModel = new TourStepsModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() =>
                tourStepsModel.DocsLink = "microsot/en-US/office/dev-program");
        }

        [Fact]
        public void ReturnEmptyStringIfDocsLinkIsEmpty()
        {
            var tourStepsModel = new TourStepsModel();
            tourStepsModel.DocsLink = "";
            Assert.Equal("", tourStepsModel.DocsLink);
        }

        #endregion
    }
}
