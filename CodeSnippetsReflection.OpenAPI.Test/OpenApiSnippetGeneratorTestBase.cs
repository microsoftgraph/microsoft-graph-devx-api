using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.Test;

public class OpenApiSnippetGeneratorTestBase
{
    private static OpenApiSnippetMetadata _v1SnippetMetadata;
    private static OpenApiSnippetMetadata _betaSnippetMetadata;
    private static IList<PowerShellCommandInfo> _commandMetadata;
    private static readonly HttpClient HttpClient = new ();
    protected const string ServiceRootUrl = "https://graph.microsoft.com/v1.0";
    protected const string ServiceRootBetaUrl = "https://graph.microsoft.com/beta";
    
    protected async static Task<OpenApiSnippetMetadata> GetV1SnippetMetadataAsync()
    {
        return _v1SnippetMetadata ??= await GetTreeNodeAsync("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/v1.0/openapi.yaml");
    }
    
    protected async static Task<OpenApiSnippetMetadata> GetBetaSnippetMetadataAsync()
    {
        return _betaSnippetMetadata ??= await GetTreeNodeAsync("https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/beta/openapi.yaml");
    }
    
    private static async Task<OpenApiSnippetMetadata> GetTreeNodeAsync(string url)
    {
        Stream stream;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            stream = await HttpClient.GetStreamAsync(url);
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
    
    protected static async Task<IList<PowerShellCommandInfo>> GetMgCommandMetadataAsync()
    {
        return _commandMetadata ??= await HttpClient.GetFromJsonAsync<IList<PowerShellCommandInfo>>(
            "https://raw.githubusercontent.com/microsoftgraph/msgraph-sdk-powershell/dev/src/Authentication/Authentication/custom/common/MgCommandMetadata.json");
    }
}
