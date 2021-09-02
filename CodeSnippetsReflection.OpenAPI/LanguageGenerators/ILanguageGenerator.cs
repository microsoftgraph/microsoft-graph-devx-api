namespace CodeSnippetsReflection.OpenAPI.LanguageGenerators {
	public interface ILanguageGenerator<T, U> where T : SnippetBaseModel<U> {
		string GenerateCodeSnippet(T snippetModel);
	}
}