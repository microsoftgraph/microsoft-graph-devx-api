using System.Text;
using System.Text.RegularExpressions;

var languages = new string [] { "csharp", "javascript", "java", "go" };
var versions = new string [] { "v1.0", "beta" };
var outputDirectory = "C:/github/microsoft-graph-devx-api/snippets-report/report";

foreach (var language in languages)
{
    foreach (var version in versions)
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine($"Processing {language} in {version}");
        Console.WriteLine("==========================================================");

        var apiReferenceFolder = $"C:/github/microsoft-graph-docs{Path.DirectorySeparatorChar}api-reference{Path.DirectorySeparatorChar}{version}{Path.DirectorySeparatorChar}api";
        var snippetsFileMap = new Dictionary<string, List<string>>();
        var snippetIncludeRegex = new Regex(@$"includes\/snippets\/{language}\/(.*)\-{language}\-snippets\.md", RegexOptions.Compiled);
        // get all .md files in the api reference folder
        var mdFiles = Directory.GetFiles(apiReferenceFolder, "*.md", SearchOption.AllDirectories);
        foreach (var mdFile in mdFiles)
        {
            // find all matches of the snippet include regex in the md file
            var matches = snippetIncludeRegex.Matches(await File.ReadAllTextAsync(mdFile).ConfigureAwait(false));
            // extract matched string
            foreach (Match match in matches)
            {
                var snippetName = match.Groups[0].Value;
                if (!snippetsFileMap.ContainsKey(snippetName))
                {
                    snippetsFileMap[snippetName] = new List<string>();
                }
                snippetsFileMap[snippetName].Add(mdFile);
            }
        }

           // print snippetsFileMap as an ordered list
        var output = new StringBuilder();
        foreach (var snippet in snippetsFileMap.OrderBy(kvp => kvp.Key))
        {
            // extract non-language-specific snippet name so that the outputs can be diffed easily
            output.AppendLine($"{snippet.Key.Replace($"includes/snippets/{language}/", string.Empty).Replace($"-{language}-snippets.md", string.Empty)}");
            foreach (var file in snippet.Value)
            {
                output.AppendLine($"    {file}");
            }
        }

        Directory.CreateDirectory(outputDirectory);
        var outputFile = $"{outputDirectory}{Path.DirectorySeparatorChar}{language}-{version}-snippets.txt";
        await File.WriteAllTextAsync(outputFile, output.ToString()).ConfigureAwait(false);
    }
}