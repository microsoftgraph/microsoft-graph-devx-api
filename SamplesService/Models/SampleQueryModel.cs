﻿// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace SamplesService.Models
{
    /// <summary>
    /// Defines a representation of a sample query.
    /// </summary>
    public class SampleQueryModel
    {
        /* Private fields */

        private string _category;
        private string _humanName;
        private string _requestUrl;
        private string _docLink;

        /* Properties */

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "category")]
        public string Category
        {
            get => _category;
            set
            {
                _category = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonProperty(Required = Required.Always, PropertyName = "method")]
        [JsonConverter(typeof(StringEnumConverter))]
        public HttpMethods Method { get; set; }

        [JsonProperty(Required = Required.Always, PropertyName = "humanName")]
        public string HumanName
        {
            get => _humanName;
            set
            {
                _humanName = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonProperty(Required = Required.Always, PropertyName = "requestUrl")]
        public string RequestUrl
        {
            get => _requestUrl;
            set
            {
                string testValue = value;
                // Check if value starts with '/' and whether there are any subsequent '/'
                if(!testValue.Trim(' ').StartsWith("/") || !testValue.TrimStart('/').Contains("/"))
                {
                    throw new ArgumentException("Invalid request url.\r\nEx.: /v1.0/me/messages", nameof(RequestUrl));
                }

                _requestUrl = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonProperty("docLink")]
        public string DocLink
        {
            get => _docLink;
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    throw new ArgumentException("URL must be absolute and valid.", nameof(DocLink));
                }

                _docLink = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonProperty("headers")]
        public IEnumerable<Header> Headers { get; set; }

        [JsonProperty("tip")]
        public string Tip { get; set; }

        [JsonProperty("postBody")]
        public string PostBody { get; set; }

        [JsonProperty("skipTest")]
        public bool SkipTest { get; set; }

        /* Nested classes */

        public class Header
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }

        /* Enums */

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
