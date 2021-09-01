using System;
using Xunit;
using CodeSnippetsReflection.OpenAPI;
using System.Net.Http;
using System.Threading.Tasks;

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
        [Fact]
        public void ThrowsOnInvalidServiceVersion() {
            var generator = new OpenApiSnippetsGenerator();
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/alpha/something")
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.ProcessPayloadRequest(requestMock, "C#"));
        }
        [Fact]
        public void DoesntThrowOnInvalidVersionWithCustomDocument() {
            var generator = new OpenApiSnippetsGenerator(customOpenApiPathOrUrl: "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/alpha/me"),
                Method = HttpMethod.Get,
            };
            Assert.NotEmpty(generator.ProcessPayloadRequest(requestMock, "C#"));
        }
        [Fact]
        public void ThrowsOnInvalidLanguage() {
            var generator = new OpenApiSnippetsGenerator();
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/v1.0/me")
            };
            Assert.Throws<ArgumentOutOfRangeException>(() => generator.ProcessPayloadRequest(requestMock, "inexistingLanguage"));
        }
    }
}
