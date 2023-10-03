using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class OpenApiSnippetGeneratorTestBase
{
    private static OpenApiSnippetMetadata _v1SnippetMetadata;
    private static OpenApiSnippetMetadata _betaSnippetMetadata;
    
    protected const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
    protected const string ServiceRootBetaUrl = "https://graph.microsoft.com/beta";
    
    protected async static Task<OpenApiSnippetMetadata> GetV1SnippetMetadata()
    {
        return _v1SnippetMetadata ??= await GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
    }
    
    protected async static Task<OpenApiSnippetMetadata> GetBetaSnippetMetadata()
    {
        return _betaSnippetMetadata ??= await GetTreeNode("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml");
    }
    
    private static async Task<OpenApiSnippetMetadata> GetTreeNode(string url)
    {
        Stream stream;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            using var httpClient = new HttpClient();
            stream = await httpClient.GetStreamAsync(url);
        }
        else
        {
            stream = File.OpenRead(url);
        }
        var openApiReaderSettings = new OpenApiReaderSettings();
        openApiReaderSettings.AddMicrosoftExtensionParsers();
        var reader = new OpenApiStreamReader(openApiReaderSettings);
        var doc = reader.Read(stream, out var diags);
        await stream.DisposeAsync();
        return new OpenApiSnippetMetadata(OpenApiUrlTreeNode.Create(doc, "default"), doc.Components.Schemas);
    }
}
