namespace CodeSnippetsReflection.OpenAPI {
    public class IndentManager {
        private int _indentLevel;
        public void Indent() => _indentLevel++;
        public void Unindent() => _indentLevel--;
        public string GetIndent() => new string('\t', _indentLevel < 0 ? 0 : _indentLevel);
    }
}