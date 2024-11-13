using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CodeSnippetsReflection.OpenAPI.LanguageGenerators;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;
using Microsoft.VisualStudio.Threading;
using UtilityService;

namespace CodeSnippetsReflection.OpenAPI
{
    public class OpenApiSnippetsGenerator : IOpenApiSnippetsGenerator
    {
        public const string treeNodeLabel = "default";
        private const string MgCommandMetadataUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/src/Authentication/Authentication/custom/common/MgCommandMetadata.json";
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(OpenApiSnippetsGenerator) } };
        private readonly AsyncLazy<OpenApiSnippetMetadata> _v1OpenApiSnippetMetadata;
        private readonly AsyncLazy<OpenApiSnippetMetadata> _betaOpenApiSnippetMetadata;
        private readonly AsyncLazy<IList<PowerShellCommandInfo>> _psCommands;
        #nullable enable
        private readonly AsyncLazy<OpenApiSnippetMetadata>? _customOpenApiSnippetMetadata;
        private static readonly JoinableTaskFactory JoinableTaskFactory = new(new JoinableTaskContext());
        private static readonly HttpClient HttpClient = new ()
        {
            Timeout = TimeSpan.FromMinutes(5),
        };
        #nullable restore
        public OpenApiSnippetsGenerator(
            string v1OpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
            string betaOpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml",
            string customOpenApiPathOrUrl = default,
            TelemetryClient telemetryClient = null)
        {
            if(string.IsNullOrEmpty(v1OpenApiDocumentUrl)) throw new ArgumentNullException(nameof(v1OpenApiDocumentUrl));
            if(string.IsNullOrEmpty(betaOpenApiDocumentUrl)) throw new ArgumentNullException(nameof(betaOpenApiDocumentUrl));

            _telemetryClient = telemetryClient;
            _v1OpenApiSnippetMetadata = new AsyncLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferencesAsync(v1OpenApiDocumentUrl), JoinableTaskFactory);
            _betaOpenApiSnippetMetadata = new AsyncLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferencesAsync(betaOpenApiDocumentUrl), JoinableTaskFactory);
            _psCommands = new AsyncLazy<IList<PowerShellCommandInfo>>(() => HttpClient.GetFromJsonAsync<IList<PowerShellCommandInfo>>(MgCommandMetadataUrl), JoinableTaskFactory);
            if(!string.IsNullOrEmpty(customOpenApiPathOrUrl))
                _customOpenApiSnippetMetadata = new AsyncLazy<OpenApiSnippetMetadata>(() => GetOpenApiReferencesAsync(customOpenApiPathOrUrl), JoinableTaskFactory);
        }
        private async Task<OpenApiSnippetMetadata> GetOpenApiReferencesAsync(string url) {
            Stream stream;
            if(url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                stream = await HttpClient.GetStreamAsync(url);
            } else {
                stream = File.OpenRead(url);
            }

            var openApiReaderSettings = new OpenApiReaderSettings();
            openApiReaderSettings.AddMicrosoftExtensionParsers();
            var reader = new OpenApiStreamReader(openApiReaderSettings);
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
        public async Task<string> ProcessPayloadRequestAsync(HttpRequestMessage requestPayload, string language)
        {
            _telemetryClient?.TrackTrace($"Generating code snippet for '{language}' from the request payload",
                                         SeverityLevel.Information,
                                         _snippetsTraceProperties);
            var (openApiSnippetMetadata, serviceRootUri) = await GetModelAndServiceUriTupleAsync(requestPayload.RequestUri);
            var snippetModel = new SnippetModel(requestPayload, serviceRootUri.AbsoluteUri, openApiSnippetMetadata);
            await snippetModel.InitializeModelAsync(requestPayload);

            var generator = await GetLanguageGeneratorAsync(language);
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
            "cli",
            "java"
        };
        private async Task<ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode>> GetLanguageGeneratorAsync(string language) {
            return language.ToLowerInvariant() switch {
                "c#" => new CSharpGenerator(),
                "typescript" => new TypeScriptGenerator(),
                "go" => new GoGenerator(),
                "powershell" => new PowerShellGenerator(await _psCommands.GetValueAsync()),
                "php" => new PhpGenerator(),
                "python" => new PythonGenerator(),
                "cli" => new GraphCliGenerator(),
                "java" => new JavaGenerator(),
                _ => throw new ArgumentOutOfRangeException($"Language '{language}' is not supported"),
            };
        }
        /// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the OpenApiUrlTreeNode and the URI of the service root</returns>
        private async Task<(OpenApiSnippetMetadata, Uri)> GetModelAndServiceUriTupleAsync(Uri requestUri)
        {
            var apiVersion = requestUri.Segments[1].Trim('/');
            var serviceRootUri = new Uri(new Uri(requestUri.GetLeftPart(UriPartial.Authority)), $"/{apiVersion}");
            return apiVersion.ToLowerInvariant() switch
            {
                "v1.0" when _customOpenApiSnippetMetadata is not null => (await _customOpenApiSnippetMetadata.GetValueAsync(), serviceRootUri),
                "v1.0" when _customOpenApiSnippetMetadata is null => (await _v1OpenApiSnippetMetadata.GetValueAsync(), serviceRootUri),
                "beta" when _customOpenApiSnippetMetadata is not null => (await _customOpenApiSnippetMetadata.GetValueAsync(), serviceRootUri),
                "beta" when _customOpenApiSnippetMetadata is null => (await _betaOpenApiSnippetMetadata.GetValueAsync(), serviceRootUri),
                _ when _customOpenApiSnippetMetadata != null => (await _customOpenApiSnippetMetadata.GetValueAsync(), serviceRootUri),
                _ => throw new ArgumentOutOfRangeException(nameof(requestUri), "Unsupported Graph version in url"),
            };
        }
    }
}
