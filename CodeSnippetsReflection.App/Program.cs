using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using CodeSnippetsReflection.OData;
using CodeSnippetsReflection.OpenAPI;

namespace CodeSnippetsReflection.App
{
    /// <summary>
    /// This is a thin layer that exposes snippet generation logic as an executable.
    /// It takes two arguments:
    /// SnippetPath: Full path to a directory which holds HTTP snippets. HTTP snippets are expected to appear
    ///             one per file where the file name ends with -httpSnippet.
    /// Languages:   Languages, comma separated.
    ///             As of this writing, values are c#, javascript, java
    ///
    /// Output is generated in the same folder as the HTTP snippets. -httpSnippet part of the file name is
    /// replaced with ---language.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var snippetsPathArg = config.GetSection("SnippetsPath");
            var languagesArg = config.GetSection("Languages");
            var customMetadataPathArg = config.GetSection("CustomMetadataPath");
            var generationArg = config.GetSection("Generation");
            if (!snippetsPathArg.Exists() || !languagesArg.Exists())
            {
                await Console.Error.WriteLineAsync("Http snippets directory and languages should be specified");
                Console.WriteLine(@"Example usage:
  .\CodeSnippetReflection.App.exe --SnippetsPath C:\snippets --Languages c#,javascript --Generation odata|openapi");
                return;
            }

            var httpSnippetsDir = snippetsPathArg.Value;
            if (!Directory.Exists(httpSnippetsDir))
            {
                await Console.Error.WriteLineAsync($@"Directory {httpSnippetsDir} does not exist!");
                return;
            }

            if (customMetadataPathArg.Exists() && !File.Exists(customMetadataPathArg.Value))
            {
                await Console.Error.WriteLineAsync($@"Metadata file {customMetadataPathArg.Value} does not exist!");
                return;
            }

            var languages = languagesArg.Value
                .Split(",")
                .Select(l => l.Trim())
                .Where(l => l != string.Empty) // eliminate trailing, leading or consecutive commas
                .Distinct();

            // splits language list into supported and unsupported languages
            // where key "true" holds supported and key "false" holds unsupported languages
            var languageGroups = languages
                .GroupBy(l => ODataSnippetsGenerator.SupportedLanguages.Contains(l) || OpenApiSnippetsGenerator.SupportedLanguages.Contains(l))
                .ToDictionary(g => g.Key, g => g.ToList());

            var supportedLanguages = languageGroups.GetValueOrDefault(true, null);
            var unsupportedLanguages = languageGroups.GetValueOrDefault(false, null);

            if (supportedLanguages == null)
            {
                await Console.Error.WriteLineAsync($"None of the given languages are supported. Supported languages: {string.Join(" ", ODataSnippetsGenerator.SupportedLanguages)}");
                return;
            }

            if (unsupportedLanguages != null)
            {
                Console.WriteLine($"Skipping these languages as they are not currently supported: {string.Join(" ", unsupportedLanguages)}");
                Console.WriteLine($"Supported languages: {string.Join(" ", ODataSnippetsGenerator.SupportedLanguages)}");
            }

            var generation = generationArg.Value;
            if(string.IsNullOrEmpty(generation))
                generation = "odata";

            var files = Directory.EnumerateFiles(httpSnippetsDir, "*-httpSnippet");

            Console.WriteLine($"Running snippet generation for these languages: {string.Join(" ", supportedLanguages)}");

            var originalGeneration = generation;

            // cache the generators by generation rather than creating a new one on each generation to avoid multiple loads of the metadata.
            var snippetGenerators = new ConcurrentDictionary<string, ISnippetsGenerator>();
            await Task.WhenAll(supportedLanguages.Select(language =>
            {
                //Generation will still be originalGeneration if language is java since it is not stable
                //Remove the condition when java is stable
                generation = (OpenApiSnippetsGenerator.SupportedLanguages.Contains(language)) ? "openapi" : originalGeneration;

                var generator = snippetGenerators.GetOrAdd(generation,  generationKey => GetSnippetsGenerator(generationKey, customMetadataPathArg));
                return files.Select(file => ProcessFileAsync(generator, language, file));
            }).SelectMany(static t => t));
            Console.WriteLine($"Processed {files.Count()} files.");
        }

        private static ISnippetsGenerator GetSnippetsGenerator(string generation, IConfigurationSection customMetadataSection) {
            return (generation) switch {
                "odata" when customMetadataSection.Exists() => new ODataSnippetsGenerator(customMetadataSection.Value),
                "odata" => new ODataSnippetsGenerator(),
                "openapi" => new OpenApiSnippetsGenerator(),
                _ => throw new InvalidOperationException($"Unknown generation type: {generation}")
            };
        }

        private static async Task ProcessFileAsync(ISnippetsGenerator generator, string language, string file)
        {
            // convert http request into a type that works with SnippetGenerator.ProcessPayloadRequest()
            // we are not altering the types as it should continue serving the HTTP endpoint as well
            using var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(await File.ReadAllTextAsync(file))));
            streamContent.Headers.Add("Content-Type", "application/http;msgtype=request");

            string snippet;
            var filePath = file.Replace("-httpSnippet", $"---{language.ToLowerInvariant()}");
            try
            {
                using var message = await streamContent.ReadAsHttpRequestMessageAsync();
                snippet = await generator.ProcessPayloadRequestAsync(message, language);
            }
            catch (Exception e)
            {
                var message = $"Exception while processing {file}.{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}";
                await Console.Error.WriteLineAsync(message);
                await File.WriteAllTextAsync(filePath + "-error", message);
                return;
            }

            if (!string.IsNullOrWhiteSpace(snippet))
            {
                Console.WriteLine($"Writing snippet: {filePath}");
                await File.WriteAllTextAsync(filePath, snippet);
            }
            else
            {
                var message = $"Failed to generate {language} snippets for {file}.";
                await File.WriteAllTextAsync(filePath + "-error", message);
                await Console.Error.WriteLineAsync(message);
            }
        }
    }
}
