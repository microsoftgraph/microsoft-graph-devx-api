using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UriMatchService
{
    public class UriTemplate
    {
        private readonly string _template;
        private Regex _ParameterRegex = null;
        private const string varname = "[a-zA-Z0-9_]*";
        private const string op = "(?<op>[+#./;?&]?)";
        private const string var = "(?<var>(?:(?<lvar>" + varname + ")[*]?,?)*)";
        private const string varspec = "(?<varspec>{" + op + var + "})";

        public UriTemplate(string template)
        {
            _template = template;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetParameters(Uri uri)
        {
            if (_ParameterRegex == null)
            {
                var matchingRegex = CreateMatchingRegex(_template);
                _ParameterRegex = new Regex(matchingRegex);
            }

            var match = _ParameterRegex.Match(uri.OriginalString);
            var parameters = new Dictionary<string, string>();

            for (int x = 1; x < match.Groups.Count; x++)
            {
                if (match.Groups[x].Success)
                {
                    var paramName = _ParameterRegex.GroupNameFromNumber(x);
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        parameters.Add(paramName, Uri.UnescapeDataString(match.Groups[x].Value));
                    }
                }
            }
            return match.Success ? parameters : null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="uriTemplate"></param>
        /// <returns></returns>
        private string CreateMatchingRegex(string uriTemplate)
        {
            var findParam = new Regex(varspec);

            var template = new Regex(@"([^{]|^)\?").Replace(uriTemplate, @"$+\?");
            var regex = findParam.Replace(template, delegate (Match m)
            {
                var paramNames = m.Groups["lvar"].Captures.Cast<Capture>().Where(c => !string.IsNullOrEmpty(c.Value)).Select(c => c.Value).ToList();
                var op = m.Groups["op"].Value;
                switch (op)
                {
                    case "?":
                        return GetQueryExpression(paramNames, prefix: "?");
                    case "&":
                        return GetQueryExpression(paramNames, prefix: "&");
                    case "#":
                        return GetExpression(paramNames, prefix: "#");
                    case "/":
                        return GetExpression(paramNames, prefix: "/");
                    case "+":
                        return GetExpression(paramNames);
                    default:
                        return GetExpression(paramNames);
                }
            });

            return "(?<!.)\\" + regex + "$"; // add negative lookbehind to strictly match this regex
        }

        private string GetQueryExpression(List<string> paramNames, string prefix)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var paramname in paramNames)
            {
                sb.Append(@"\" + prefix + "?");
                if (prefix == "?")
                {
                    prefix = "&";
                }

                sb.Append("(?:");
                sb.Append(paramname);
                sb.Append("=");

                sb.Append("(?<");
                sb.Append(paramname);
                sb.Append(">");
                sb.Append("[^/?&]+");
                sb.Append(")");
                sb.Append(")?");
            }

            return sb.ToString();
        }

        private string GetExpression(List<string> paramNames, string prefix = null)
        {
            StringBuilder sb = new StringBuilder();

            string paramDelim;

            switch (prefix)
            {
                case "#":
                    paramDelim = "[^,]+";
                    break;
                case "/":
                    paramDelim = "[^/?]+";
                    break;
                case "?":
                case "&":
                    paramDelim = "[^&#]+";
                    break;
                case ";":
                    paramDelim = "[^;/?#]+";
                    break;
                case ".":
                    paramDelim = "[^./?#]+";
                    break;

                default:
                    paramDelim = "[^/?&]+";
                    break;
            }

            foreach (var paramname in paramNames)
            {
                if (string.IsNullOrEmpty(paramname)) continue;

                if (prefix != null)
                {
                    sb.Append(@"\" + prefix + "?");
                    if (prefix == "#") { prefix = ","; }
                }
                sb.Append("(?<");
                sb.Append(paramname);
                sb.Append(">");
                sb.Append(paramDelim); // Param Value
                sb.Append(")?");
            }

            return sb.ToString();
        }
    }
}
