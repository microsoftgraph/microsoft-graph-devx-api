namespace CodeSnippetsReflection.OpenAPI {
	internal static class StringExtensions {
		internal static bool IsCollectionIndex(this string pathSegment) =>
			!string.IsNullOrEmpty(pathSegment) && pathSegment.StartsWith('{') && pathSegment.EndsWith('}');
	}
}