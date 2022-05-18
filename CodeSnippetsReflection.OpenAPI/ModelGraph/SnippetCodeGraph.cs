using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph
{
    public class SnippetCodeGraph
    {
        public OpenApiSchema ResponseSchema
        {
            get; set;
        }
        public HttpMethod HttpMethod
        {
            get; set;
        }

        public IEnumerable<CodeProperty> Headers
        {
            get; set;
        }
        public IEnumerable<CodeProperty> Options
        {
            get; set;
        }

        public IEnumerable<CodeProperty> Parameters
        {
            get; set;
        }

        public CodeProperty Body
        {
            get; set;
        }

        public IEnumerable<OpenApiUrlTreeNode> Nodes
        {
            get; set;
        }

       
        public Boolean HasHeaders()
        {
            return Headers.Any() == true;
        }

        public Boolean HasOptions()
        {
            return Options.Any() == true;
        }

        public Boolean HasParameters()
        {
            return Parameters.Any() == true;
        }

        public Boolean HasBody()
        {
            return Body.PropertyType != PropertyType.Default;
        }

    }
}
