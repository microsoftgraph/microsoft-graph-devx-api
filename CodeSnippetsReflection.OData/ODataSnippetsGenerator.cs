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

namespace CodeSnippetsReflection.OData
{
    /// <summary>
    /// Snippets Generator Class with all the logic for code generation
    /// </summary>
    public class ODataSnippetsGenerator : IODataSnippetsGenerator
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(ODataSnippetsGenerator) } };
        private static readonly HashSet<string> supportedLanguages = new()
        {
            "c#",
            "javascript",
            "objective-c",
            "java"
        };

        private Lazy<IEdmModel> IedmModelV1 { get; set; }
        private Lazy<IEdmModel> IedmModelBeta { get; set; }

        /// <summary>
        /// initialized only if custom metadata path is specified in constructor
        /// </summary>
        private Lazy<IEdmModel> CustomEdmModel { get; set; }

        private Uri ServiceRootV1 { get; set; }
        private Uri ServiceRootBeta { get; set; }
        private JavascriptExpressions JavascriptExpressions { get; }
        private CSharpExpressions CSharpExpressions { get; }
        private ObjectiveCExpressions ObjectiveCExpressions { get; }
        private JavaExpressions JavaExpressions { get; }

        /// <summary>
        /// Determines whether the snippet generation is running through the command line interface
        /// (as opposed to DevX HTTP API)
        /// </summary>
        private readonly bool IsCommandLine;

        /// <summary>
        /// Class holding the Edm model and request processing for snippet generations
        /// </summary>
        /// <param name="isCommandLine">Determines whether we are running the snippet generation in command line</param>
        /// <param name="customMetadataPath">Full file path to the metadata</param>
        public ODataSnippetsGenerator(bool isCommandLine = false, string customMetadataPath = null, TelemetryClient telemetryClient = null)
        {
            _telemetryClient = telemetryClient;
            IsCommandLine = isCommandLine;
            LoadGraphMetadata(customMetadataPath);
            JavascriptExpressions = new JavascriptExpressions();
            CSharpExpressions = new CSharpExpressions();
            ObjectiveCExpressions = new ObjectiveCExpressions();
            JavaExpressions = new JavaExpressions();
        }

        /// <summary>
        /// Load the IEdmModel for both V1 and Beta
        /// </summary>
        /// <param name="customMetadataPath">Full file path to the metadata</param>
        private void LoadGraphMetadata(string customMetadataPath)
        {
            ServiceRootV1 = new Uri(UtilityConstants.ServiceRootV1);
            ServiceRootBeta = new Uri(UtilityConstants.ServiceRootBeta);

            // use clean metadata
            IedmModelV1 = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(UtilityConstants.CleanV1Metadata)));
            IedmModelBeta = new Lazy<IEdmModel>(() => CsdlReader.Parse(XmlReader.Create(UtilityConstants.CleanBetaMetadata)));

            if (customMetadataPath == null)
            {
                return;
            }

            if (!File.Exists(customMetadataPath))
            {
                throw new FileNotFoundException("Metadata file is not found in the specified path!", nameof(customMetadataPath));
            }

            CustomEdmModel = new Lazy<IEdmModel>(() =>
            {
                using (var reader = File.OpenText(customMetadataPath))
                {
                    return CsdlReader.Parse(XmlReader.Create(reader));
                }
            });
        }

        /// <summary>
        /// Entry point to generate snippets from the payload
        /// </summary>
        /// <param name="language"></param>
        /// <param name="httpRequestMessage"></param>
        /// <returns>String of snippet generated</returns>
        public string ProcessPayloadRequest(HttpRequestMessage httpRequestMessage, string language)
        {
            var (edmModel, serviceRootUri) = GetModelAndServiceUriTuple(httpRequestMessage.RequestUri);
            var snippetModel = new SnippetModel(httpRequestMessage, serviceRootUri.AbsoluteUri, edmModel);

            _telemetryClient?.TrackTrace($"Generating code snippet for '{language}' from the request payload",
                                         SeverityLevel.Information,
                                         _snippetsTraceProperties);

            switch (language.ToLower())
            {
                case "c#":
                    var csharpGenerator = new CSharpGenerator(edmModel, IsCommandLine);
                    return csharpGenerator.GenerateCodeSnippet(snippetModel, CSharpExpressions);

                case "javascript":
                    return JavaScriptGenerator.GenerateCodeSnippet(snippetModel, JavascriptExpressions);

                case "objective-c":
                    var objectiveCGenerator = new ObjectiveCGenerator(edmModel);
                    return objectiveCGenerator.GenerateCodeSnippet(snippetModel, ObjectiveCExpressions);

                case "java":
                    var javaGenerator = new JavaGenerator(edmModel);
                    return javaGenerator.GenerateCodeSnippet(snippetModel, JavaExpressions);

                default:
                    throw new Exception("Invalid Language selected");
            }
        }

        /// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the Edm model and the URI of the service root</returns>
        private (IEdmModel, Uri) GetModelAndServiceUriTuple(Uri requestUri)
        {
            switch (requestUri.Segments[1])
            {
                case "v1.0/":
                    return ((CustomEdmModel ?? IedmModelV1).Value, ServiceRootV1);

                case "beta/":
                    return ((CustomEdmModel ?? IedmModelBeta).Value, ServiceRootBeta);

                default:
                    throw new Exception("Unsupported Graph version in url");
            }

        }
    }
}
