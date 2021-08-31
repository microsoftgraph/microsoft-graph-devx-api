using System;
using Xunit;
using CodeSnippetsReflection.OpenAPI;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public class OpenApiSnippetsGeneratorTests
    {
        [Fact]
        public void DefensiveProgramming()
        {
            Assert.Throws<ArgumentNullException>(() => new OpenApiSnippetsGenerator(null));
            Assert.Throws<ArgumentNullException>(() => new OpenApiSnippetsGenerator("something", null));
            Assert.NotNull(new OpenApiSnippetsGenerator("something", "something"));
        }
    }
}
