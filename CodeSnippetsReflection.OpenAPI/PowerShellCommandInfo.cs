using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSnippetsReflection.OpenAPI
{
    public record PowerShellCommandInfo
    {
        public string Method { get; set; }
        public string OutputType { get; set; }
        public string Uri { get; set; }
        public string Command { get; set; }
        public string Module { get; set; }
        public string ApiVersion { get; set; }
    }
}
