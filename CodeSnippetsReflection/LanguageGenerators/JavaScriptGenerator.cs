using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

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
                //setup the auth snippet section
                snippetBuilder.Append("// Some callback function\n");
                snippetBuilder.Append("const authProvider: AuthProvider = (callback: AuthProviderCallback) => { \n");
                snippetBuilder.Append("\t// Your logic for getting and refreshing accessToken \n");
                snippetBuilder.Append("\t// Error should be passed in case of error while authenticating \n");
                snippetBuilder.Append("\t// accessToken should be passed upon successful authentication \n");
                snippetBuilder.Append("\tcallback(error, accessToken);\n};\n\n");
                snippetBuilder.Append("let options: Options = {\n");
                snippetBuilder.Append("\t\tauthProvider,\n};\n\n");
                //init the client
                snippetBuilder.Append("const client = Client.init(options);\n\n");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    snippetBuilder.Append("const client = Client.init(options);\n\n");
                    snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = client.api('{snippetModel.Path}')");
                    //append beta
                    snippetBuilder.Append(BetaSectionString(snippetModel.ApiVersion));
                    snippetBuilder.Append(CommonGenerator.GenerateQuerySection(snippetModel, languageExpressions));
                    //append footer
                    snippetBuilder.Append("\n\t.get();");
                    
                }
                else if (snippetModel.Method == HttpMethod.Post)
                {
                    var name = snippetModel.ResponseVariableName;
                    //remove the quotation marks from the JSON keys
                    var pattern = "\"(.*?) *\":";
                    var javascriptObject = Regex.Replace(snippetModel.RequestBody,pattern, "$1:");
                    snippetBuilder.Append($"const {name} = {javascriptObject};");
                    snippetBuilder.Append("\r\n\r\n");
                    snippetBuilder.Append($"client.api('{snippetModel.Path}')");
                    //append beta
                    snippetBuilder.Append(BetaSectionString(snippetModel.ApiVersion));
                    //parameter for the post.
                    name = "{" + name + " : " + name + "}";
                    snippetBuilder.Append($".post({name});");

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
            return apiVersion.Equals("beta") ? "\n\t.version('beta')" : "";
        }
    }

    internal class JavascriptExpressions : LanguageExpressions
    {
        public override string FilterExpression => "\n\t.filter('{0}')"; 
        public override string SearchExpression => "\n\t.search('{0}')"; 
        public override string ExpandExpression => "\n\t.expand('{0}')"; 
        public override string SelectExpression => "\n\t.select('{0}')"; 
        public override string OrderByExpression => "\n\t.orderby('{0}')"; 
        public override string SkipExpression => "\n\t.skip('{0}')"; 
        public override string SkipTokenExpression  => "\n\t.skiptoken('{0}')"; 
        public override string TopExpression => "\n\t.top('{0}')"; 

        public override string FilterExpressionDelimiter => ",";

        public override string ExpandExpressionDelimiter => ",";

        public override string SelectExpressionDelimiter => ",";

        public override string OrderByExpressionDelimiter => " ";

        public override string HeaderExpression => "\n\t.header('{0}','{1}')";
    }
}
