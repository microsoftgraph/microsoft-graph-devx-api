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
	public class OpenAPISnippetsGenerator : IOpenAPISnippetsGenerator
	{
		public const string treeNodeLabel = "default";
		private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(OpenAPISnippetsGenerator) } };
		private readonly Lazy<OpenApiUrlTreeNode> _v1OpenApiDocument;
		private readonly Lazy<OpenApiUrlTreeNode> _betaOpenApiDocument;
		private readonly Lazy<OpenApiUrlTreeNode> _customOpenApiDocument;
		public OpenAPISnippetsGenerator(
			string v1OpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
			string betaOpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml",
			string customOpenApiPathOrUrl = default,
			TelemetryClient telemetryClient = null)
		{
			if(string.IsNullOrEmpty(v1OpenApiDocumentUrl)) throw new ArgumentNullException(nameof(v1OpenApiDocumentUrl));
			if(string.IsNullOrEmpty(betaOpenApiDocumentUrl)) throw new ArgumentNullException(nameof(betaOpenApiDocumentUrl));

			_telemetryClient = telemetryClient;
			_v1OpenApiDocument = new Lazy<OpenApiUrlTreeNode>(() => GetOpenApiDocument(v1OpenApiDocumentUrl).GetAwaiter().GetResult());
			_betaOpenApiDocument = new Lazy<OpenApiUrlTreeNode>(() => GetOpenApiDocument(betaOpenApiDocumentUrl).GetAwaiter().GetResult());
			if(!string.IsNullOrEmpty(customOpenApiPathOrUrl))
				_customOpenApiDocument = new Lazy<OpenApiUrlTreeNode>(() => GetOpenApiDocument(customOpenApiPathOrUrl).GetAwaiter().GetResult());
		}
		private static async Task<OpenApiUrlTreeNode> GetOpenApiDocument(string url) {
			Stream stream;
			if(url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) {
				using var httpClient = new HttpClient();
				stream = await httpClient.GetStreamAsync(url);
			} else {
				stream = File.OpenRead(url);
			}
			var reader = new OpenApiStreamReader();
			var doc = reader.Read(stream, out var diags);
			await stream.DisposeAsync();
			if(diags.Errors.Any())
				throw new InvalidOperationException($"Failed to load the OpenAPI document:{Environment.NewLine}{diags.Errors.Select(x => x.Message).Aggregate((x, y) => x + Environment.NewLine + y)}");
			return OpenApiUrlTreeNode.Create(doc, treeNodeLabel);
		}
		public string ProcessPayloadRequest(HttpRequestMessage requestPayload, string language)
		{
			_telemetryClient?.TrackTrace($"Generating code snippet for '{language}' from the request payload",
                                         SeverityLevel.Information,
                                         _snippetsTraceProperties);
			var (openApiTreeNode, serviceRootUri) = GetModelAndServiceUriTuple(requestPayload.RequestUri);
			var snippetModel = new SnippetModel(requestPayload, serviceRootUri.AbsoluteUri, openApiTreeNode);

			var generator = GetLanguageGenerator(language);
			return generator.GenerateCodeSnippet(snippetModel);
		}
		private static ILanguageGenerator<SnippetModel, OpenApiUrlTreeNode> GetLanguageGenerator(string language) {
			return (language.ToLowerInvariant()) switch {
				"c#" => new CSharpGenerator(),
				_ => throw new ArgumentOutOfRangeException($"Language '{language}' is not supported"),
			};
		}
		private static readonly Dictionary<string, ILanguageGenerator<SnippetBaseModel<object>, object>> _languageGenerators = new () {
			{ "c#", new CSharpGenerator() as ILanguageGenerator<SnippetBaseModel<object>, object> },
		};
		/// <summary>
        /// Helper function to select the appropriate EDM model and service root url depending on the request made
        /// </summary>
        /// <param name="requestUri">The URI of the service requested</param>
        /// <returns>Tuple of the OpenApiUrlTreeNode and the URI of the service root</returns>
        private (OpenApiUrlTreeNode, Uri) GetModelAndServiceUriTuple(Uri requestUri)
        {
			var serviceRootUri = new Uri(requestUri.GetLeftPart(UriPartial.Authority) + "/" + requestUri.Segments.First());
            switch (requestUri.Segments[1].Trim('/').ToLowerInvariant())
            {
                case "v1.0":
                    return ((_customOpenApiDocument ?? _v1OpenApiDocument).Value, serviceRootUri);

                case "beta":
                    return ((_customOpenApiDocument ?? _betaOpenApiDocument).Value, serviceRootUri);

                default:
                    throw new Exception("Unsupported Graph version in url");
            }

        }
	}
}
