using System;
using Xunit;

namespace CodeSnippetsReflection.Test
{
    public class SnippetsGeneratorShould
    {
        [Fact]
        public void ReturnResources()
        {
            //Arange
            SnippetsGenerator snippetGenerator = new SnippetsGenerator();

            //Act
            string actualCSharpSnippetCode = snippetGenerator.GenerateCsharpSnippet(null, "");

            // "/v1.0/me/messages
            string expectedCodeSnippetOutput = "var messages = await graphClient.Me.Messages.Request().GetAsync();";

             //Assert
            Assert.Equal(expectedCodeSnippetOutput, actualCSharpSnippetCode);
        }

        [Fact]
        public void NavigateToAnEntityInACollection()
        {
            //Arrange
            SnippetsGenerator snippetGenerator = new SnippetsGenerator();
            
            //Act
            string actualCSharpSnippetCode = snippetGenerator.GenerateCsharpSnippet(null, "");

            // "/v1.0/me/messages("event-message-id")
            string expectedCodeSnippetOutput = "var messages = await graphClient.Me.Messages[\"event-message-id\"].Request().GetAsync();";

             //Assert
            Assert.Equal(expectedCodeSnippetOutput, actualCSharpSnippetCode);
        }

        [Fact]
        public void ParseODataFunction()
        {
            //Arrange
            SnippetsGenerator snippetGenerator = new SnippetsGenerator();

            //Act
            string actualCSharpSnippetCode = snippetGenerator.GenerateCsharpSnippet(null, "");

            // "/v1.0/me/outlook/supportedTimeZones";
            string expectedCodeSnippetOutput = "var outlook = await graphClient.Me.Outlook.SupportedTimeZones().Request().GetAsync();";
            
            //Assert
            Assert.Equal(expectedCodeSnippetOutput, actualCSharpSnippetCode);
        }

        [Fact]
        public void ParseODataAction()
        {
            
        }
    }
}
