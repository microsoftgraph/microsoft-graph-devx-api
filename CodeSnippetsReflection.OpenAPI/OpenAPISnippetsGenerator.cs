using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using UtilityService;

namespace CodeSnippetsReflection.OpenAPI
{
    public class OpenApiSnippetsGenerator : IOpenApiSnippetsGenerator
    {
        public const string treeNodeLabel = "default";
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(OpenApiSnippetsGenerator) } };
        private readonly SimpleLazy<OpenApiSnippetMetadata> _v1OpenApiSnippetMetadata;
        private readonly SimpleLazy<OpenApiSnippetMetadata> _betaOpenApiSnippetMetadata;
        private readonly SimpleLazy<OpenApiSnippetMetadata> _customOpenApiSnippetMetadata;
        public OpenApiSnippetsGenerator(
            string v1OpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
            string betaOpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml",
            string customOpenApiPathOrUrl = default,
            TelemetryClient telemetryClient = null)
        {
            if(string.IsNullOrEmpty(v1OpenApiDocumentUrl)) throw new ArgumentNullException(nameof(v1OpenApiDocumentUrl));
            if(string.IsNullOrEmpty(betaOpenApiDocumentUrl)) throw new ArgumentNullException(nameof(betaOpenApiDocumentUrl));

            _telemetryClient = telemetryClient;
            _v1OpenApiSnippetMetadata = new SimpleLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferences(v1OpenApiDocumentUrl).GetAwaiter().GetResult());
            _betaOpenApiSnippetMetadata = new SimpleLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferences(betaOpenApiDocumentUrl).GetAwaiter().GetResult());
            if(!string.IsNullOrEmpty(customOpenApiPathOrUrl))
                _customOpenApiSnippetMetadata = new SimpleLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferences(customOpenApiPathOrUrl).GetAwaiter().GetResult());
        }
        private async Task<OpenApiSnippetMetadata> GetOpenApiReferences(string url) {
            Stream stream;
            if(url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                stream = await httpClient.GetStreamAsync(url);
            } else {
                stream = File.OpenRead(url);
            }
            var reader = new OpenApiStreamReader();
            var doc = reader.Read(stream, out var diags);
            await stream.DisposeAsync();
            
            if(doc == null)
                throw new InvalidOperationException("Failed to load the OpenAPI document");
            
            if (diags.Errors.Any())
            {
                _telemetryClient?.TrackTrace($"Parsed OpenAPI with errors. {doc?.Paths?.Count ?? 0} paths found.", SeverityLevel.Error);
                foreach (var parsingError in diags.Errors)
                {
                    _telemetryClient?.TrackTrace($"OpenAPI error: {parsingError.Pointer} - {parsingError.Message}", SeverityLevel.Error);
                }
            }
            return new OpenApiSnippetMetadata(OpenApiUrlTreeNode.Create(doc, treeNodeLabel), doc.Components.Schemas) ;
        }
        public string ProcessPayloadRequest(HttpRequestMessage requestPayload, string language)
        {
            _telemetryClient?.TrackTrace($"Generating code snippet for '{language}' from the request payload",
                                         SeverityLevel.Information,
                                         _snippetsTraceProperties);
            var (openApiSnippetMetadata, serviceRootUri) = GetModelAndServiceUriTuple(requestPayload.RequestUri);
            var snippetModel = new SnippetModel(requestPayload, serviceRootUri.AbsoluteUri, openApiSnippetMetadata);

            var generator = GetLanguageGenerator(language);
            return generator.GenerateCodeSnippet(snippetModel);
        }
        public static HashSet<string> SupportedLanguages { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "c#",
            "typescript",
            "go",
            "powershell",
            "php",
            "python",
            "cli"
        };
        private static ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode> GetLanguageGenerator(string language) {
            return language.ToLowerInvariant() switch {
                "c#" => new CSharpGenerator(),
                "typescript" => new TypeScriptGenerator(),
                "go" => new GoGenerator(),
                "powershell" => new PowerShellGenerator(),
                "php" => new PhpGenerator(),
                "python" => new PythonGenerator(),
                "cli" => new GraphCliGenerator(),
                _ => throw new ArgumentOutOfRangeException($"Language '{language}' is not supported"),
            };
        }
        /// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the OpenApiUrlTreeNode and the URI of the service root</returns>
        private (OpenApiSnippetMetadata, Uri) GetModelAndServiceUriTuple(Uri requestUri)
        {
            var apiVersion = requestUri.Segments[1].Trim('/');
            var serviceRootUri = new Uri(new Uri(requestUri.GetLeftPart(UriPartial.Authority)), $"/{apiVersion}");
            return apiVersion.ToLowerInvariant() switch
            {
                "v1.0" => ((_customOpenApiSnippetMetadata ?? _v1OpenApiSnippetMetadata).Value, serviceRootUri),
                "beta" => ((_customOpenApiSnippetMetadata ?? _betaOpenApiSnippetMetadata).Value, serviceRootUri),
                _ when _customOpenApiSnippetMetadata != null => (_customOpenApiSnippetMetadata.Value, serviceRootUri),
                _ => throw new ArgumentOutOfRangeException(nameof(requestUri), "Unsupported Graph version in url"),
            };
        }
    }
}
