using System.Linq;
using System.Text.RegularExpressions;
using CodeSnippetsReflection.StringExtensions;

namespace CodeSnippetsReflection.OpenAPI;

internal static class StringExtensions 
{
    internal static bool IsCollectionIndex(this string pathSegment) =>
        !string.IsNullOrEmpty(pathSegment) && pathSegment.StartsWith('{') && pathSegment.EndsWith('}');
    internal static bool IsFunction(this string pathSegment) => !string.IsNullOrEmpty(pathSegment) && pathSegment.Contains('.');
    internal static string ReplaceValueIdentifier(this string original) =>
        original?.Replace("$value", "Content");

    internal static string Append(this string original, string suffix) =>
        string.IsNullOrEmpty(original) ? original : original + suffix;
        
    private static readonly Regex PropertyCleanupRegex = new(@"[""\s!#$%&'()*+,./:;<=>?@\[\]\\^`{}|~-](?<followingLetter>\w)?", RegexOptions.Compiled);
    private const string CleanupGroupName = "followingLetter";
    internal static string CleanupSymbolName(this string original)
    {
        if (string.IsNullOrEmpty(original))
            return original;

        var result = PropertyCleanupRegex.Replace(original, static x => x.Groups.Keys.Contains(CleanupGroupName) ? 
            x.Groups[CleanupGroupName].Value.ToFirstCharacterUpperCase() :
            string.Empty); //strip out any invalid characters, and replace any following one by its uppercase version

        return result;
    }
}
