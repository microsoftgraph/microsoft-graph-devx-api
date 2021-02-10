using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("CodeSnippetsReflection.Test")]
namespace CodeSnippetsReflection.LanguageGenerators
{
    public static class JavaScriptGenerator
    {
        /// <summary>
        /// Formulates the requested Graph snippets and returns it as string for JavaScript
        /// </summary>
        /// <param name="snippetModel">Model of the Snippets info <see cref="SnippetModel"/></param>
        /// <param name="languageExpressions">The language expressions to be used for code Gen</param>
        /// <returns>String of the snippet in Javascript code</returns>
        public static string GenerateCodeSnippet(SnippetModel snippetModel , LanguageExpressions languageExpressions)
        {
            try
            {
                var snippetBuilder = new StringBuilder();
                snippetModel.ResponseVariableName = CommonGenerator.EnsureVariableNameIsNotReserved(snippetModel.ResponseVariableName,languageExpressions);
                //setup the auth snippet section
                snippetBuilder.Append("const options = {\r\n");
                snippetBuilder.Append("\tauthProvider,\r\n};\r\n\r\n");
                //init the client
                snippetBuilder.Append("const client = Client.init(options);\r\n\r\n");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    //append any queries with the actions
                    var getActions = CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions) + "\r\n\t.get();";
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, getActions));
                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    if (!string.IsNullOrEmpty(snippetModel.RequestBody))
                    {
                        snippetBuilder.Append(JavascriptGenerateObjectFromJson(snippetModel.RequestBody, snippetModel.ResponseVariableName));
                        snippetBuilder.Append(GenerateRequestSection(snippetModel, $"\r\n\t.post({snippetModel.ResponseVariableName});"));
                    }
                    else
                    {
                        snippetBuilder.Append(GenerateRequestSection(snippetModel, "\r\n\t.post();"));
                    }
                }
                else if (snippetModel.Method == HttpMethod.Patch)
                {
                    if (string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception("No body present for PATCH method in Javascript");

                    snippetBuilder.Append(JavascriptGenerateObjectFromJson(snippetModel.RequestBody, snippetModel.ResponseVariableName));
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"\r\n\t.update({snippetModel.ResponseVariableName});"));
                }
                else if (snippetModel.Method == HttpMethod.Delete)
                {
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, "\r\n\t.delete();"));
                }
                else if (snippetModel.Method == HttpMethod.Put)
                {
                    if(string.IsNullOrEmpty(snippetModel.RequestBody))
                        throw new Exception("No body present for PUT method in Javascript");

                    snippetBuilder.Append(JavascriptGenerateObjectFromJson(snippetModel.RequestBody, snippetModel.ResponseVariableName));
                    snippetBuilder.Append(GenerateRequestSection(snippetModel, $"\r\n\t.put({snippetModel.ResponseVariableName});"));
                }
                else
                {
                    throw new NotImplementedException("HTTP method not implemented for Javascript");
                }

                return snippetBuilder.ToString();
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Return string to signifying the endpoint to be used in the snippet
        /// </summary>
        /// <param name="apiVersion">Api version of the request</param>
        /// <returns>String of the snippet in JS code</returns>
        private static string BetaSectionString(string apiVersion)
        {
            return apiVersion.Equals("beta",StringComparison.Ordinal) ? "\r\n\t.version('beta')" : "";
        }

        /// <summary>
        /// Generate the snippet section that makes the call to the api with the surrounding try catch block
        /// </summary>
        /// <param name="snippetModel">Snippet model built from the request</param>
        /// <param name="actions">String of actions to be done inside the code block</param>
        private static string GenerateRequestSection(SnippetModel snippetModel, string actions)
        {
            var stringBuilder = new StringBuilder();
            var path = snippetModel.Path;
            if (snippetModel.CustomQueryOptions.Any())
            {
                //just append the query string since its a custom query
                path += snippetModel.QueryString;
            }
            stringBuilder.Append($"let res = await client.api('{path}')");
            //append beta
            stringBuilder.Append(BetaSectionString(snippetModel.ApiVersion));
            stringBuilder.Append(actions);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generate a valid declaration of a json object from the json string. This essentially involves stripping out the quotation marks 
        /// out of the json keys
        /// </summary>
        /// <param name="jsonBody">Json body to generate object from</param>
        /// <param name="variableName">name of variable to use</param>
        /// <returns>String of the snippet of the json object declaration in JS code</returns>
        private static string JavascriptGenerateObjectFromJson(string jsonBody , string variableName)
        {
            if (string.IsNullOrEmpty(jsonBody))
                return "";//nothing to generate with no body

            var stringBuilder = new StringBuilder();
            //remove the quotation marks from the JSON keys
            const string pattern = "\"(.*?) *\":";
            var javascriptObject = Regex.Replace(jsonBody.Trim(), pattern, "$1:");
            stringBuilder.Append($"const {variableName} = {javascriptObject};");
            stringBuilder.Append("\r\n\r\n");
            return stringBuilder.ToString();
        }
    }

    internal class JavascriptExpressions : LanguageExpressions
    {
        public override string FilterExpression => "\r\n\t.filter('{0}')"; 
        public override string SearchExpression => "\r\n\t.search('{0}')"; 
        public override string ExpandExpression => "\r\n\t.expand('{0}')"; 
        public override string SelectExpression => "\r\n\t.select('{0}')"; 
        public override string OrderByExpression => "\r\n\t.orderby('{0}')"; 
        public override string SkipExpression => "\r\n\t.skip({0})"; 
        public override string SkipTokenExpression  => "\r\n\t.skiptoken('{0}')"; 
        public override string TopExpression => "\r\n\t.top({0})"; 

        public override string FilterExpressionDelimiter => ",";

        public override string SelectExpressionDelimiter => ",";

        public override string OrderByExpressionDelimiter => " ";

        public override string HeaderExpression => "\r\n\t.header('{0}','{1}')";

        public override string[] ReservedNames => new string [] {
            "await","abstract", "arguments", "boolean", "break", "byte", "case",
            "catch","char", "const", "continue", "debugger", "default", "delete",
            "do","double", "else", "enum","export","extends","eval", "false", "final",
            "finally", "float", "for","function", "goto", "if", "implements", "in",
            "instanceof", "int","interface", "let", "long", "native", "new", "null",
            "package","private", "protected", "public", "return", "short", "static",
            "switch","synchronized", "this", "throw", "throws", "transient", "true",
            "try","typeof", "var", "void", "volatile", "while", "with", "yield" };

        public override string ReservedNameEscapeSequence => "_";

        public override string DoubleQuotesEscapeSequence => "\"";
    }
}
