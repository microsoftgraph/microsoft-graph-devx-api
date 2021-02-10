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
    }
}
