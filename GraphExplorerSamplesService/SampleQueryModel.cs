using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraphExplorerSamplesService
{
    public class SampleQueryModel
    {
        [JsonProperty(Required = Required.Always)]
        public Guid Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Category { get; set; }

        [JsonProperty(Required = Required.Always)]
        public HttpMethods Method { get; set; }

        [JsonProperty(Required = Required.Always)]                   
        public string HumanName
        {
            get { return HumanName; }
            set
            {
                if (value.Length > 64)
                { throw new ArgumentOutOfRangeException(nameof(HumanName), "The maximum length allowed is 64 characters."); }

                HumanName = value;
            }
        } 

        [JsonProperty(Required = Required.Always)]
        public string RequestUrl { get; set; }       

        public string DocLink
        {
            get { return DocLink; }
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                { throw new ArgumentException(nameof(DocLink), "URL must be absolute and valid."); }

                DocLink = value;
            }
        }

        public IEnumerable<Header> Headers { get; set; }

        public string Tip { get; set; }

        public string PostBody { get; set; }

        public bool SkipTest { get; set; }


        public class Header
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }


        public enum HttpMethods
        {
            GET,
            PUT,
            POST,
            DELETE,
            PATCH
        }
    }
}
