using System;
using Xunit;
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
        public async Task ThrowsOnInvalidServiceVersionAsync() {
            var generator = new OpenApiSnippetsGenerator();
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/alpha/something")
            };
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => generator.ProcessPayloadRequestAsync(requestMock, "javascript"));
        }
        [Fact]
        public async Task DoesntThrowOnInvalidVersionWithCustomDocumentAsync() {
            var generator = new OpenApiSnippetsGenerator(customOpenApiPathOrUrl: "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/alpha/me"),
                Method = HttpMethod.Get,
            };
            Assert.NotEmpty(await generator.ProcessPayloadRequestAsync(requestMock, "C#"));
        }
        [Fact]
        public async Task ThrowsOnInvalidLanguageAsync() {
            var generator = new OpenApiSnippetsGenerator();
            using var requestMock = new HttpRequestMessage {
                RequestUri = new Uri("https://graph.microsoft.com/v1.0/me")
            };
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => generator.ProcessPayloadRequestAsync(requestMock, "inexistingLanguage"));
        }
    }
}
