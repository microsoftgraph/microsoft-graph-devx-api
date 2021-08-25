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
            Assert.Throws<ArgumentNullException>(() => new OpenAPISnippetsGenerator(null));
            Assert.Throws<ArgumentNullException>(() => new OpenAPISnippetsGenerator("something", null));
            Assert.NotNull(new OpenAPISnippetsGenerator("something", "something"));
        }
    }
}
