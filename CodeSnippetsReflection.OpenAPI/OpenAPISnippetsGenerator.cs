using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CodeSnippetsReflection;
using Microsoft.ApplicationInsights;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using UtilityService;

namespace CodeSnippetsReflection.OpenAPI
{
	public class OpenAPISnippetsGenerator : IOpenAPISnippetsGenerator
	{
		private readonly TelemetryClient _telemetryClient;
        private readonly Dictionary<string, string> _snippetsTraceProperties =
                    new() { { UtilityConstants.TelemetryPropertyKey_Snippets, nameof(OpenAPISnippetsGenerator) } };
        public static HashSet<string> SupportedLanguages = new HashSet<string>
        {
            "c#",
        };
		private readonly Lazy<OpenApiDocument> _v1OpenApiDocument;
		private readonly Lazy<OpenApiDocument> _betaOpenApiDocument;
		public OpenAPISnippetsGenerator(
			string v1OpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml",
			string betaOpenApiDocumentUrl = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml")
		{
			if(string.IsNullOrEmpty(v1OpenApiDocumentUrl)) throw new ArgumentNullException(nameof(v1OpenApiDocumentUrl));
			if(string.IsNullOrEmpty(betaOpenApiDocumentUrl)) throw new ArgumentNullException(nameof(betaOpenApiDocumentUrl));
			_v1OpenApiDocument = new Lazy<OpenApiDocument>(() => GetOpenApiDocument(v1OpenApiDocumentUrl).GetAwaiter().GetResult());
			_betaOpenApiDocument = new Lazy<OpenApiDocument>(() => GetOpenApiDocument(betaOpenApiDocumentUrl).GetAwaiter().GetResult());
		}
		private static async Task<OpenApiDocument> GetOpenApiDocument(string url) {
			using var httpClient = new HttpClient();
			using var response =  await httpClient.GetAsync(url);
			if(response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Failed to get the OpenAPI document: {url} returned {response.StatusCode}");
			using var stream = await response.Content.ReadAsStreamAsync();
			var reader = new OpenApiStreamReader();
			var doc = reader.Read(stream, out var diags);
			if(diags.Errors.Any())
				throw new InvalidOperationException($"Failed to load the OpenAPI document:{Environment.NewLine}{diags.Errors.Select(x => x.Message).Aggregate((x, y) => x + Environment.NewLine + y)}");
			return doc;
		}
		public string ProcessPayloadRequest(HttpRequestMessage requestPayload, string language)
		{
			throw new NotImplementedException();
		}
	}
}
