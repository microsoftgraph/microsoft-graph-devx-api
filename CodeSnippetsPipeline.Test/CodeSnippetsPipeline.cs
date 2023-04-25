namespace CodeSnippetsPipeline.Test;

[TestFixture]
public class CodeSnippetsPipeline
{
    public static IEnumerable<TestCaseData> TestData => GetTestData();

    private static IEnumerable<TestCaseData> GetTestData()
    {
        var getEnv = (string name) => Environment.GetEnvironmentVariable(name) ?? throw new ArgumentNullException($"env:{name}");

        // enumerate all HTTP snippets in the folder
        var snippetsPath = getEnv("SNIPPETS_PATH");
        var language = getEnv("SNIPPET_LANGUAGE").ToLowerInvariant();
        var version = getEnv("GRAPH_VERSION");
        var snippets = Directory.EnumerateFiles(snippetsPath, $"*{version}-httpSnippet*", SearchOption.AllDirectories);

        foreach(var snippetFullPath in snippets)
        {
            var snippetName = Path.GetFileNameWithoutExtension(snippetFullPath).Replace("-httpSnippet", "");
            var testCase = new TestCaseData(snippetFullPath, language);
            testCase.SetName(snippetName).SetCategory(nameof(CodeSnippetsPipeline));
            yield return testCase;
        }
    }

    [Test]
    [Category(nameof(CodeSnippetsPipeline))]
    [TestCaseSource(typeof(CodeSnippetsPipeline), nameof(TestData))]
    public void Test(string httpSnippetFilePath, string language)
    {
        var fileName = Path.GetFileName(httpSnippetFilePath);
        if (language.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            if (fileName.EndsWith("-httpSnippet", StringComparison.Ordinal))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail(File.ReadAllText(httpSnippetFilePath));
            }
        }
        else
        {
            var expectedLanguageSnippetFileFullPath = string.Concat(httpSnippetFilePath.AsSpan(0, httpSnippetFilePath.LastIndexOf("-httpSnippet", StringComparison.Ordinal)), $"---{language}");
            if (File.Exists(expectedLanguageSnippetFileFullPath))
            {
                Assert.Pass();
            }
            else
            {
                var expectedLanguageSnippetErrorFileFullPath = string.Concat(expectedLanguageSnippetFileFullPath, "-error");
                if (File.Exists(expectedLanguageSnippetErrorFileFullPath))
                {
                    var message = "Original HTTP Snippet:" + Environment.NewLine + 
                        File.ReadAllText(httpSnippetFilePath).TrimStart() + Environment.NewLine +
                        File.ReadAllText(expectedLanguageSnippetErrorFileFullPath);
                    Assert.Fail(message);
                }         
                else
                {
                    Assert.Fail("Snippet file is not generated and exception message is not captured");
                }
            }
        }
    }
}
