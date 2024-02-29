// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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

        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("category"), JsonRequired]
        public string Category
        {
            get => _category;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _category = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonPropertyName("method"), JsonRequired, JsonConverter(typeof(JsonStringEnumConverter))]
        public HttpMethods Method
        {
            get; set;
        }

        [JsonPropertyName("humanName"), JsonRequired]
        public string HumanName
        {
            get => _humanName;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _humanName = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonPropertyName("requestUrl"), JsonRequired]
        public string RequestUrl
        {
            get => _requestUrl;
            set
            {
                string testValue = value ?? throw new ArgumentNullException(nameof(RequestUrl));
                // Check if value starts with '/' and whether there are any subsequent '/'
                if(!testValue.Trim(' ').StartsWith('/') || !testValue.TrimStart('/').Contains('/'))
                {
                    throw new ArgumentException("Invalid request url.\r\nEx.: /v1.0/me/messages", nameof(RequestUrl));
                }

                _requestUrl = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }

        [JsonPropertyName("docLink")]
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

        [JsonPropertyName("headers")]
        public IEnumerable<Header> Headers { get; set; }

        [JsonPropertyName("tip")]
        public string Tip { get; set; }

        [JsonPropertyName("postBody")]
        public string PostBody { get; set; }

        [JsonPropertyName("skipTest")]
        public bool SkipTest { get; set; }

        /* Nested classes */

        public class Header
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("value")]
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
