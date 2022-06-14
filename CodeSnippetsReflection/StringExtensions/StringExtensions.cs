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

        public static string ToFirstCharacterUpperCaseAfterCharacter(this string stringValue, char character)
        {
            if (string.IsNullOrEmpty(stringValue)) return stringValue;
            int charIndex = stringValue.IndexOf(character);
            if (charIndex < 0) return stringValue;
            return stringValue[0..charIndex] + char.ToUpper(stringValue[charIndex + 1]) + stringValue[(charIndex + 2)..].ToFirstCharacterUpperCaseAfterCharacter(character);
        }

        public static string EscapeQuotes(this string stringValue)
        {
            return stringValue.Replace("\"", "\\\"");
        }

        public static string AddQuotes(this string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue)) return stringValue;
            if (stringValue.Substring(0, 1) == "\"") return stringValue;

            return $"\"{stringValue}\"";
        }


       /// <summary>
        /// Add an integer suffix to a string. All string from position 1 will have a suffix appended
        /// </summary>
        /// <param name="stringValue">The string value to escape</param>
        /// <param name="position">Poistion of the string</param>
        /// <param name="singleQuoteEscapeSequence">The string value to replace single-quotes</param>
        /// <returns></returns>
        public static string IndexSuffix(this string stringValue, int position)
        {
            if (string.IsNullOrEmpty(stringValue)) return stringValue;

            return $"{stringValue}{(position > 0 ? position : null)}";
        }
    }
}
