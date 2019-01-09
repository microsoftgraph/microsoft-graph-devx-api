using System;
using System.Collections.Generic;
using System.Text;

namespace CodeSnippetsReflection
{
    internal class InputSnippet
    {
        internal InputSnippet(string httpVerb, string urlToResource)
        {
            this.HttpVerb = UppercaseFirstLetter(httpVerb.ToLower());
            this.UrlToResource = urlToResource;            
        }

      
        internal string HttpVerb { get; set; }
        internal string UrlToResource { get; set; }

        /// <summary>
        /// Use this one as its faster.
        /// https://www.dotnetperls.com/uppercase-first-letter
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UppercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static string LowercaseFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToLower(a[0]);
            return new string(a);
        }
    }

  

}
