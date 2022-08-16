namespace CodeSnippetsReflection.Report;

public record SnippetsSummaryEntry(
    string SnippetCanonicalName,
    string Language,
    string DocsFile   
)
{
    public override string ToString() => $"{SnippetCanonicalName},{Language},{DocsFile}";
};
