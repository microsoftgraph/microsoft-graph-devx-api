using System.Text;
using System.Linq;
using System;
using System.Text.RegularExpressions;


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
        private static readonly Regex ImportPathRegex = new Regex(@"(\w+)[A-Z]?(\w*)\((\w+Id)='(\{[^{}]+\})'\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
        private static readonly Regex SnakeCaseRegex = new Regex(@"(\B[A-Z])", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

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

        public static string ReplaceMultiple(this string stringValue, string haystack, params string []needles)
        {
            if (string.IsNullOrEmpty(stringValue)) return stringValue;
            if (needles == null || needles.Length == 0) return stringValue;
            foreach (var needle in needles)
            {
                if (string.IsNullOrEmpty(needle) || !stringValue.Contains(needle, StringComparison.InvariantCulture)) continue;
                stringValue = stringValue.Replace(needle, haystack, StringComparison.OrdinalIgnoreCase);
            }
            return stringValue;
        }

        public static string ToFirstCharacterUpperCaseAfterCharacter(this string stringValue, char character)
        {
            if (string.IsNullOrEmpty(stringValue)) return stringValue;
            int charIndex = stringValue.IndexOf(character);
            if (charIndex < 0) return stringValue;
            return stringValue[0..charIndex] + char.ToUpper(stringValue[charIndex + 1]) + stringValue[(charIndex + 2)..].ToFirstCharacterUpperCaseAfterCharacter(character);
        }

        public static string ToPascalCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            string[] words = str.Split('_');
            string pascalCaseString = string.Join("", words.Select(ToFirstCharacterUpperCase));
            return pascalCaseString;
        }

        public static string ToSnakeCase(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            StringBuilder snakeCaseBuilder = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        snakeCaseBuilder.Append('_');
                    }
                    snakeCaseBuilder.Append(char.ToLower(c));
                }
                else if (c=='.')
                {
                    snakeCaseBuilder.Append('_');
                }
                
                else
                {
                    snakeCaseBuilder.Append(c);
                }
            }
            return snakeCaseBuilder.ToString();
        }


        public static string CleanUpImportPath(this string input)
            {
                string result = StringExtensions.ImportPathRegex.Replace(input, m =>
                {
                    string firstPart = m.Groups[1].Value;
                    string secondPart = m.Groups[2].Value;
                    string idPart = m.Groups[3].Value;

                    // Given Id e.g appIdd, groupID - convert to snake case
                    secondPart = StringExtensions.SnakeCaseRegex.Replace(secondPart, x => "_" + x.Value.ToLower());
                    idPart = StringExtensions.SnakeCaseRegex.Replace(idPart, x => "_" + x.Value.ToLower());

                    return $"{firstPart}_{secondPart}_with_{idPart}";
                });

                result = result.Replace("$", "");

                return result;
            }

            
        public static string EscapeQuotes(this string stringValue)
        {
            return stringValue.Replace("\\\"", "\"")//try to unescape quotes in case the input string is already escaped to avoid double escaping.
                              .Replace("\"", "\\\"");
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
