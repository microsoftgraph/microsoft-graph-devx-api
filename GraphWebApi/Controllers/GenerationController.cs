using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using GraphWebApi.Models;
using Kiota.Builder;
using Kiota.Builder.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GraphWebApi.Controllers;

[ApiController]
public class GenerationController : ControllerBase, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;
    public GenerationController(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = configuration;
        _loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
        //TODO why are we still using serilogs object model?
        //TODO move openapi file to configuration
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Route("generate")]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody]GenerationOptions options, CancellationToken cancellationToken)
    {
        if(options is null)
        {
            return BadRequest();
        }
        if (string.IsNullOrEmpty(options.ClientNamespaceName))
        {
            return BadRequest("ClientNamespaceName is required");
        }
        if (string.IsNullOrEmpty(options.ClientClassName))
        {
            return BadRequest("ClientClassName is required");
        }
        var output = Path.Combine(Path.GetTempPath(), "kiota", Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
        var generationConfiguration = new GenerationConfiguration {
            Language = (Kiota.Builder.GenerationLanguage)options.GenerationLanguage,
            ClientNamespaceName = options.ClientNamespaceName,
            ClientClassName = options.ClientClassName,
            IncludePatterns = options.IncludePatterns,
            ExcludePatterns = options.ExcludePatterns,
            OutputPath = output,
            UsesBackingStore = true,
            OpenAPIFilePath = $"https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/openapi/{options.Version}/openapi.yaml"
        };
        var generator = new KiotaBuilder(_loggerFactory.CreateLogger<KiotaBuilder>(), generationConfiguration);
        await generator.GenerateClientAsync(cancellationToken);
        var outStream = new MemoryStream();// don't dispose, it is needed for the response
        var zipStream = new ZipArchive(outStream, ZipArchiveMode.Create, true);
        foreach(var file in Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = zipStream.CreateEntry(file[(output.Length + 1)..]);
            using var entryStream = entry.Open();
            using var fileStream = System.IO.File.OpenRead(file);
            await fileStream.CopyToAsync(entryStream, cancellationToken);
            await fileStream.FlushAsync(cancellationToken);
            await entryStream.FlushAsync(cancellationToken);
        }
        zipStream.Dispose();// writes the checksum
        await outStream.FlushAsync(cancellationToken);
        outStream.Seek(0, SeekOrigin.Begin);
        return File(outStream, "application/zip", "client.zip");
    }
}
