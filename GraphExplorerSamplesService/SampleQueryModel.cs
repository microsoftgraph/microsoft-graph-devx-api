using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GraphExplorerSamplesService
{
    public class SampleQueryModel
    {
        // Field data for the custom mutator methods
        private string _humanName;
        private string _docLink;

        /* Properties */

        [JsonProperty(Required = Required.Always)]
        public Guid Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Category { get; set; }

        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public HttpMethods Method { get; set; }
        
        [JsonProperty(Required = Required.Always)]                   
        public string HumanName
        {
            get { return _humanName; }
            set
            {
                if (value.Length > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(HumanName), "The maximum length allowed is 64 characters.");
                }

                _humanName = value;
            }
        } 

        [JsonProperty(Required = Required.Always)]
        public string RequestUrl { get; set; }
                
        public string DocLink
        {
            get { return _docLink; }
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    throw new ArgumentException(nameof(DocLink), "URL must be absolute and valid.");
                }

                _docLink = value;
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
