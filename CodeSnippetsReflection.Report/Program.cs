using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using CodeSnippetsReflection.Report;

IConfiguration config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

string docsRepoLocation = string.Empty;

try
{
    docsRepoLocation = config.GetRequiredSection("docsRepo").Value;
}
catch(InvalidOperationException)
{
    Console.Error.WriteLine("Please specify where the docs repository is located");
    Environment.Exit(1);
}

var apiReferencePath = Path.Combine(docsRepoLocation, "api-reference");

if(!Directory.Exists(apiReferencePath))
{
    Console.Error.WriteLine("Docs repository not found at {0}", docsRepoLocation);
    Environment.Exit(1);
}

var languages = new string[] { "csharp", "javascript", "java", "go" };
var versions = new string[] { "v1.0", "beta" };

foreach (var version in versions)
{
    var snippetSummary = new List<SnippetsSummaryEntry>();
    var apiReferenceVersionPath = Path.Combine(apiReferencePath, version);
    var apiReferenceApiPath = Path.Combine(apiReferenceVersionPath, "api");
    var apiReferenceFiles = Directory.GetFiles(apiReferenceApiPath, "*.md", SearchOption.AllDirectories);
    foreach (var language in languages)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine($"Processing {language} in {version}");
        Console.WriteLine("==========================================================");

        var snippetsFileMap = new Dictionary<string, List<string>>();
        var snippetIncludeRegex = new Regex(@$"includes\/snippets\/{language}\/(.*)\-{language}\-snippets\.md", RegexOptions.Compiled);
        
        foreach (var apiReferenceFile in apiReferenceFiles)
        {
            var matches = snippetIncludeRegex.Matches(await File.ReadAllTextAsync(apiReferenceFile).ConfigureAwait(false));
            foreach (Match match in matches.Cast<Match>())
            {
                var snippetName = match.Groups[0].Value;
                if (!snippetsFileMap.ContainsKey(snippetName))
                {
                    snippetsFileMap[snippetName] = new List<string>();
                }
                snippetsFileMap[snippetName].Add(apiReferenceFile);
            }
        }

        foreach (var snippet in snippetsFileMap)
        {
            var snippetFile = snippet.Key.Replace($"includes/snippets/{language}/", string.Empty);
            var snippetCanonicalName = snippetFile.Replace($"-{language}-snippets.md", string.Empty);
            foreach (var apiReferenceFileFullPath in snippet.Value)
            {
                var apiReferenceFile = Path.GetFileName(apiReferenceFileFullPath);
                snippetSummary.Add(new SnippetsSummaryEntry(snippetCanonicalName, language, apiReferenceFile));
            }
        }
    }

    var snippetSummaryFile = Path.Combine(apiReferenceVersionPath, "includes", "snippets-summary.csv");
    var orderedSnippetSummary = snippetSummary.OrderBy(entry => entry.SnippetCanonicalName).ThenBy(entry => entry.Language);
    await File.WriteAllTextAsync(snippetSummaryFile, "SnippetCanonicalName,Language,DocsFile"
        + Environment.NewLine
        + string.Join(Environment.NewLine, orderedSnippetSummary)
        ).ConfigureAwait(false);
}
