namespace CodeSnippetsReflection.OpenAPI {
    internal static class StringExtensions {
        internal static bool IsCollectionIndex(this string pathSegment) =>
            !string.IsNullOrEmpty(pathSegment) && pathSegment.StartsWith('{') && pathSegment.EndsWith('}');
        internal static bool IsFunction(this string pathSegment) => !string.IsNullOrEmpty(pathSegment) && pathSegment.Contains('.');
        internal static string ReplaceValueIdentifier(this string original) =>
            original?.Replace("$value", "Content");
    }
}
