using GraphExplorerSamplesService.Models;
using System;
using Xunit;

namespace SamplesService.Test
{
    public class SampleQueryModelShould
    {
        #region Category Property Test

        [Fact]
        public void ThrowArgumentOutOfRangeExceptionIfInvalidCategoryIsSet()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => sampleQueryModel.Category = "foobar");
        }

        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromCategoryValueIfPresent()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act
            sampleQueryModel.Category = "   Users    "; // leading and trailing whitespaces

            // Assert
            Assert.False(sampleQueryModel.Category.StartsWith(" "));
            Assert.False(sampleQueryModel.Category.EndsWith(" "));
            Assert.Equal("Users", sampleQueryModel.Category);
        }

        [Fact]
        public void BuildStringOfCategories()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act
            string stringOfCategories = sampleQueryModel.BuildStringOfCategories();

            // Assert
            Assert.NotNull(stringOfCategories);
        }

        #endregion

        #region HumanName Property Test

        [Fact]
        public void ThrowArgumentOutOfRangeExceptionIfHumanNamePropertyIsSetToMoreThan64Characters()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                sampleQueryModel.HumanName = @"abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz");
        }

        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromHumanNameValueIfPresent()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act
            sampleQueryModel.HumanName = "   my profile    "; // leading and trailing whitespaces

            // Assert
            Assert.False(sampleQueryModel.HumanName.StartsWith(" "));
            Assert.False(sampleQueryModel.HumanName.EndsWith(" "));
            Assert.Equal("my profile", sampleQueryModel.HumanName);
        }

        #endregion

        #region Request Url Property Test

        [Fact]
        public void ThrowArgumentExceptionIfRequestUrlPropertyIsSetToInvalidValue()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => 
                sampleQueryModel.RequestUrl = "v1.0/me/photo/$value"); // Missing starting slash 
            Assert.Throws<ArgumentException>(() => 
                sampleQueryModel.RequestUrl = "/v1.0mephoto$value"); // Missing subsequent slash 
        }

        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromRequestUrlValueIfPresent()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act
            sampleQueryModel.RequestUrl = "  /v1.0/me/photo/$value  "; // leading and trailing whitespaces

            // Assert
            Assert.False(sampleQueryModel.RequestUrl.StartsWith(" "));
            Assert.False(sampleQueryModel.RequestUrl.EndsWith(" "));
            Assert.Equal("/v1.0/me/photo/$value", sampleQueryModel.RequestUrl);
        }

        #endregion

        #region DocLink Property Test

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsSetToInvalidUriValue()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => 
                sampleQueryModel.DocLink = "microsoft/en-us/graph/docs/api-reference/v1.0/resources/users");            
        }

        [Fact]
        public void ThrowArgumentExceptionIfDocLinkPropertyIsNotSetToAbsoluteUriValue()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act and Assert
            Assert.Throws<ArgumentException>(() => 
                sampleQueryModel.DocLink = "developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users");
        }

        [Fact]
        public void TrimAllLeadingAndTrailingWhiteSpacesFromDocLinkValueIfPresent()
        {
            // Arrange
            SampleQueryModel sampleQueryModel = new SampleQueryModel();

            // Act
            sampleQueryModel.DocLink = "   https://developer.microsoft.com/en-us/graph/docs/api-reference/v1.0/resources/users    ";

            // Assert
            Assert.False(sampleQueryModel.DocLink.StartsWith(" "));
            Assert.False(sampleQueryModel.DocLink.EndsWith(" "));
        }

        #endregion

    }
}
