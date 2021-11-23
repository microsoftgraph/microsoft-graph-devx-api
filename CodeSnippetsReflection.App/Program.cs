using System;
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
    ///             As of this writing, values are c#, javascript, objective-c, java
    ///
    /// Output is generated in the same folder as the HTTP snippets. -httpSnippet part of the file name is
    /// replaced with ---language.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
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
                Console.Error.WriteLine("Http snippets directory and languages should be specified");
                Console.WriteLine(@"Example usage:
  .\CodeSnippetReflection.App.exe --SnippetsPath C:\snippets --Languages c#,javascript --Generation odata|openapi");
                return;
            }

            var httpSnippetsDir = snippetsPathArg.Value;
            if (!Directory.Exists(httpSnippetsDir))
            {
                Console.Error.WriteLine($@"Directory {httpSnippetsDir} does not exist!");
                return;
            }

            if (customMetadataPathArg.Exists() && !File.Exists(customMetadataPathArg.Value))
            {
                Console.Error.WriteLine($@"Metadata file {customMetadataPathArg.Value} does not exist!");
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

            var supportedLanguages = languageGroups.ContainsKey(true) ? languageGroups[true] : null;
            var unsupportedLanguages = languageGroups.ContainsKey(false) ? languageGroups[false] : null;

            if (supportedLanguages == null)
            {
                Console.Error.WriteLine($"None of the given languages are supported. Supported languages: {string.Join(" ", ODataSnippetsGenerator.SupportedLanguages)}");
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

            var pathNotFoundInOpenAPISB = new StringBuilder();
            var snippetsFoundSB = new StringBuilder();
            var commandNotFoundSB = new StringBuilder();

            File.Delete("F:\\test\\PathNotFoundInOpenAPI.txt");
            File.Delete("F:\\test\\SnippetsFound.txt");
            File.Delete("F:\\test\\CommandNotFound.txt");

            var originalGeneration = generation;

            Parallel.ForEach(supportedLanguages, language =>
            {
                if(language.Equals("go", StringComparison.OrdinalIgnoreCase))
                    generation = "openapi";
                else
                    generation = originalGeneration;
                var generator = GetSnippetsGenerator(generation, customMetadataPathArg);
                Parallel.ForEach(files, file =>
                {
                    ProcessFile(generator, language, file, pathNotFoundInOpenAPISB, snippetsFoundSB, commandNotFoundSB);
                });
            });
            Console.WriteLine($"Processed {files.Count()} files.");
            File.AppendAllText("F:\\test\\PathNotFoundInOpenAPI.txt", pathNotFoundInOpenAPISB.ToString());
            File.AppendAllText("F:\\test\\SnippetsFound.txt", snippetsFoundSB.ToString());
            File.AppendAllText("F:\\test\\CommandNotFound.txt", commandNotFoundSB.ToString());

            pathNotFoundInOpenAPISB.Clear();
            snippetsFoundSB.Clear();
            commandNotFoundSB.Clear();
        }

        private static ISnippetsGenerator GetSnippetsGenerator(string generation, IConfigurationSection customMetadataSection) {
            return (generation) switch {
                "odata" when customMetadataSection.Exists() => new ODataSnippetsGenerator(isCommandLine: true, customMetadataSection.Value),
                "odata" => new ODataSnippetsGenerator(isCommandLine: true),
                "openapi" => new OpenApiSnippetsGenerator(),
                _ => throw new InvalidOperationException($"Unknown generation type: {generation}")
            };
        }

        private static void ProcessFile(ISnippetsGenerator generator, string language, string file, StringBuilder pathNotFoundInOpenAPISB, StringBuilder snippetsFoundSB, StringBuilder commandNotFoundSB)
        {
        //}

        //private static void ProcessFile(ISnippetsGenerator generator, string language, string file)
        //{
            // convert http request into a type that works with SnippetGenerator.ProcessPayloadRequest()
            // we are not altering the types as it should continue serving the HTTP endpoint as well
            using var streamContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(file))));
            streamContent.Headers.Add("Content-Type", "application/http;msgtype=request");

            string snippet;
            try
            {
                // This is a very fast operation, it is fine to make is synchronuous.
                // With the parallel foreach in the main method, processing all snippets for C# in both Beta and V1 takes about 7 seconds.
                // As of this writing, the code was processing 2650 snippets
                // Using async-await is costlier as this operation is all in-memory and task creation and scheduling overhead is high for that.
                // With async-await, the same operation takes 1 minute 7 seconds.
                using var message = streamContent.ReadAsHttpRequestMessageAsync().Result;
                snippet = generator.ProcessPayloadRequest(message, language);
             }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception while processing {file}.{Environment.NewLine}{e.Message}{Environment.NewLine}{e.StackTrace}");
                pathNotFoundInOpenAPISB.AppendLine(file);
                return;
            }

            if (snippet != string.Empty)
            {
                var filePath = file.Replace("-httpSnippet", $"---{language.ToLowerInvariant()}");
                Console.WriteLine($"Writing snippet: {filePath}");
                File.WriteAllText(filePath, snippet);
                snippetsFoundSB.AppendLine(filePath);
            }
            else
            {
                Console.WriteLine($"Failed to generate {language} snippets for {file}.");
                commandNotFoundSB.AppendLine(file);
            }
        }
    }
}
