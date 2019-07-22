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
        [StringLength(64, ErrorMessage = "Maximum number of characters allowed is 64")]            
        public string HumanName { get; set; } 

        [JsonProperty(Required = Required.Always)]
        public string RequestUrl { get; set; }

        [Url(ErrorMessage = "Invalid URL")]
        public string DocLink { get; set; }

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
