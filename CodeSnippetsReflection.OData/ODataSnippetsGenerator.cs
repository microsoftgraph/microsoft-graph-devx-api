using System;
using System.IO;
using System.Net.Http;
using System.Xml;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using CodeSnippetsReflection.OData.LanguageGenerators;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using UtilityService;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace CodeSnippetsReflection.OData
{
    /// <summary>
    /// Snippets Generator Class with all the logic for code generation
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ODataSnippetsGenerator : IODataSnippetsGenerator
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(ODataSnippetsGenerator) } };

        private static readonly JoinableTaskFactory _joinableTaskFactory = new(new JoinableTaskContext());
        private AsyncLazy<IEdmModel> IedmModelV1 { get; set; }
        private AsyncLazy<IEdmModel> IedmModelBeta { get; set; }
        private static readonly HttpClient HttpClient = new ()
        {
            Timeout = TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// initialized only if custom metadata path is specified in constructor
        /// </summary>
        private AsyncLazy<IEdmModel> CustomEdmModel { get; set; }

        private Uri ServiceRootV1 { get; set; }
        private Uri ServiceRootBeta { get; set; }
        private JavascriptExpressions JavascriptExpressions { get; }
        public static HashSet<string> SupportedLanguages { get; set; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "javascript",
        };

        /// <summary>
        /// Class holding the Edm model and request processing for snippet generations
        /// </summary>
        /// <param name="customMetadataPath">Full file path to the metadata</param>
        public ODataSnippetsGenerator(string customMetadataPath = null, TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            LoadGraphMetadata(customMetadataPath);
            JavascriptExpressions = new JavascriptExpressions();
        }

        /// <summary>
        /// Load the IEdmModel for both V1 and Beta
        /// </summary>
        /// <param name="customMetadataPath">Full file path to the metadata</param>
        [ExcludeFromCodeCoverage]
        private void LoadGraphMetadata(string customMetadataPath)
        {
            ServiceRootV1 = new Uri(UtilityConstants.ServiceRootV1);
            ServiceRootBeta = new Uri(UtilityConstants.ServiceRootBeta);

            // use clean metadata
            IedmModelV1 = new AsyncLazy<IEdmModel>(() => GetEdmModelAsync(UtilityConstants.CleanV1Metadata), _joinableTaskFactory);
            IedmModelBeta = new AsyncLazy<IEdmModel>(() => GetEdmModelAsync(UtilityConstants.CleanBetaMetadata), _joinableTaskFactory);
            if (customMetadataPath == null)
            {
                return;
            }

            if (!File.Exists(customMetadataPath))
            {
                throw new FileNotFoundException("Metadata file is not found in the specified path!", nameof(customMetadataPath));
            }
            CustomEdmModel = new AsyncLazy<IEdmModel>(() => GetEdmModelAsync(customMetadataPath), _joinableTaskFactory);
        }
        private static async Task<IEdmModel> GetEdmModelAsync(string url) {
            Stream stream;
            if(url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
                stream = await HttpClient.GetStreamAsync(url);
            } else {
                stream = File.OpenRead(url);
            }
            return CsdlReader.Parse(XmlReader.Create(stream)) ;
        }
        /// <summary>
        /// Entry point to generate snippets from the payload
        /// </summary>
        /// <param name="language"></param>
        /// <param name="requestPayload"></param>
        /// <returns>String of snippet generated</returns>
        public async Task<string> ProcessPayloadRequestAsync(HttpRequestMessage requestPayload, string language)
        {
            var (edmModel, serviceRootUri) = await GetModelAndServiceUriTupleAsync(requestPayload.RequestUri);
            var snippetModel = new SnippetModel(requestPayload, serviceRootUri.AbsoluteUri, edmModel);
            await snippetModel.InitializeModelAsync(requestPayload);

            _telemetryClient?.TrackTrace($"Generating code snippet for '{language}' from the request payload",
                                         SeverityLevel.Information,
                                         _snippetsTraceProperties);

            switch (language.ToLower())
            {
                case "javascript":
                    return JavaScriptGenerator.GenerateCodeSnippet(snippetModel, JavascriptExpressions);
                default:
                    throw new ArgumentOutOfRangeException($"Invalid Language {language} selected");
            }
        }

        /// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the Edm model and the URI of the service root</returns>
        private async Task<(IEdmModel, Uri)> GetModelAndServiceUriTupleAsync(Uri requestUri)
        {
            return requestUri.Segments[1] switch
            {
                "v1.0/" => (await (CustomEdmModel ?? IedmModelV1).GetValueAsync(), ServiceRootV1),
                "beta/" => (await (CustomEdmModel ?? IedmModelBeta).GetValueAsync(), ServiceRootBeta),
                _ => throw new ArgumentOutOfRangeException(nameof(requestUri), "Unsupported Graph version in url"),
            };
        }
    }
}
