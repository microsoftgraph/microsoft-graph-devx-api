// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UriMatchingService
{
    /// <summary>
    /// Provides utility for matching a given uri against
    /// a list of pre-defined uri templates.
    /// </summary>
    public class UriTemplateMatcher
    {
        private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();

        public void Add(string key, string template)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), Constants.ValueNullOrEmpty);
            }
            if (string.IsNullOrEmpty(template))
            {
                throw new ArgumentNullException(nameof(template), Constants.ValueNullOrEmpty);
            }

            _templates.Add(key, template);
        }

        /// <summary>
        /// Matches a uri against the list of uri templates defined
        /// in the template table.
        /// </summary>
        /// <param name="uri">The uri to match.</param>
        /// <returns>A <see cref="TemplateMatch"/> if a match is found, else a null result.</returns>
        public TemplateMatch Match(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri), Constants.ValueNull);
            }

            Uri absolutePath = uri;
            if (uri.IsAbsoluteUri)
            {
                absolutePath = new Uri(uri.AbsolutePath, UriKind.Relative);
            }

            foreach (var template in _templates)
            {
                var parameters = GetParameters(absolutePath, template.Value);
                if (parameters != null)
                {
                    return new TemplateMatch() { Key = template.Key, Template = template.Value };
                }
            }
            return null;
        }

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException(nameof(key), Constants.ValueNullOrEmpty);
                }

                if (_templates.TryGetValue(key, out string value))
                {
                    return value;
                }
                else
                {
                    return null;
                }
            }
        }

        private IDictionary<string, string> GetParameters(Uri uri, string template)
        {
            Regex parameterRegex = null;

            if (parameterRegex == null)
            {
                var matchingRegex = CreateMatchingRegex(template);
                parameterRegex = new Regex(matchingRegex);
            }

            var match = parameterRegex.Match(uri.OriginalString);
            var parameters = new Dictionary<string, string>();

            for (int x = 1; x < match.Groups.Count; x++)
            {
                if (match.Groups[x].Success)
                {
                    var paramName = parameterRegex.GroupNameFromNumber(x);
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        parameters.Add(paramName, Uri.UnescapeDataString(match.Groups[x].Value));
                    }
                }
            }
            return match.Success ? parameters : null;
        }

        private string CreateMatchingRegex(string uriTemplate)
        {
            var findParam = new Regex(Constants.VarSpec);

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

    /// <summary>
    /// Defines properties of a uri template match.
    /// </summary>
    public class TemplateMatch
    {
        public string Key { get; set; }
        public string Template { get; set; }
    }
}

