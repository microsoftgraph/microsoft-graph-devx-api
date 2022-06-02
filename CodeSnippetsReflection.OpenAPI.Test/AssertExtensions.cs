using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeSnippetsReflection.OpenAPI.Test
{
    public static class AssertExtensions
    {
        public static void ContainsIgnoreWhiteSpace(string expectedSubstring, string actualString)
        {
            Xunit.Assert.Contains(
                expectedSubstring.Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty),
                actualString.Replace(" ", string.Empty).Replace(Environment.NewLine, string.Empty)
            );
        }
    }
}
