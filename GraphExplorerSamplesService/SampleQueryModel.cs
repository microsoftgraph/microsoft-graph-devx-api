using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        public string HumanName { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string RequestUrl { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string DocLink { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string Tip { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public bool SkipTest { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public string PostBody { get; set; }

        [JsonProperty(Required = Required.AllowNull)]
        public List<Header> Headers { get; set; }

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
