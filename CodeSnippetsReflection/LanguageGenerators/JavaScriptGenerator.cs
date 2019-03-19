using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace CodeSnippetsReflection.LanguageGenerators
{
    public class JavaScriptGenerator
    {
        public string GenerateCodeSnippet(SnippetModel snippetModel)
        {
            try
            {
                var snippetBuilder = new StringBuilder();
                //setup the auth 
                snippetBuilder.Append("// Some callback function\n");
                snippetBuilder.Append("const authProvider: AuthProvider = (callback: AuthProviderCallback) => { \n");
                snippetBuilder.Append("\t// Your logic for getting and refreshing accessToken \n");
                snippetBuilder.Append("\t// Error should be passed in case of error while authenticating \n");
                snippetBuilder.Append("\t// accessToken should be passed upon successful authentication \n");
                snippetBuilder.Append("\tcallback(error, accessToken);\n};\n\n");
                snippetBuilder.Append("let options: Options = {\n");
                snippetBuilder.Append("\t\tauthProvider,\n};\n\n");
                snippetBuilder.Append("const client = Client.init(options);\n\n");

                snippetBuilder.Append($"var {snippetModel.ResponseVariableName} = client.api('{snippetModel.Path}')");

                if (snippetModel.Method == HttpMethod.Get)
                {
                    snippetBuilder.Append(CommonGenerator.GenerateQuerySection(snippetModel, new JavascriptExpressions()));

                    snippetBuilder.Append("\n\t.get();");

                    return snippetBuilder.ToString();
                }
                else
                {
                    throw new NotImplementedException("HTTP method not implemented");
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

    class JavascriptExpressions : LanguageExpressions
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
    }
}
