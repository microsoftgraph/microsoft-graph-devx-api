namespace CodeSnippetsReflection.StringExtensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a new string with all double-quotes (") and single-quotes (')
        /// escaped with the specified values
        /// </summary>
        /// <param name="stringLiteral">The string value to escape</param>
        /// <param name="doubleQuoteEscapeSequence">The string value to replace double-quotes</param>
        /// <param name="singleQuoteEscapeSequence">The string value to replace single-quotes</param>
        /// <returns></returns>
        public static string EscapeQuotesInLiteral(this string stringLiteral, 
                                                   string doubleQuoteEscapeSequence,
                                                   string singleQuoteEscapeSequence)
        {
            return stringLiteral
                .Replace("\"", doubleQuoteEscapeSequence)
                .Replace("'", singleQuoteEscapeSequence);
        }
        public static string ToFirstCharacterLowerCase(this string stringValue) {
            if(string.IsNullOrEmpty(stringValue)) return stringValue;
            return char.ToLower(stringValue[0]) + stringValue[1..];
        }
        public static string ToFirstCharacterUpperCase(this string stringValue) {
            if(string.IsNullOrEmpty(stringValue)) return stringValue;
            return char.ToUpper(stringValue[0]) + stringValue[1..];
        }
    }
}
