using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphExplorerSamplesService.Models
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
                // Remove all leading and trailing whitespaces before comparing
                if (!SampleQueriesCategories.CategoriesLinkedList.Contains(value.Trim(' ')))
                {
                    throw new ArgumentOutOfRangeException(nameof(Category), 
                        "The category specified does not exist in the defined list of categories.\r\nAllowable values:\r\n" + BuildStringOfCategories());
                }

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
                if (value.Length > 64)
                {
                    throw new ArgumentOutOfRangeException(nameof(HumanName), "The maximum length allowed is 64 characters.");
                }

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
                    throw new ArgumentException(nameof(RequestUrl), "Invalid request url.\r\nEx.: /v1.0/me/messages");
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
                    throw new ArgumentException(nameof(DocLink), "URL must be absolute and valid.");
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

        /* Methods */

        /// <summary>
        /// Creates a string of the list of categories.
        /// </summary>
        /// <returns>A string of list of categories.</returns>
        public string BuildStringOfCategories()
        {
            StringBuilder categoriesStringCollection = new StringBuilder();

            foreach (string category in SampleQueriesCategories.CategoriesLinkedList)
            {
                categoriesStringCollection.Append(category + "\r\n");
            }

            return categoriesStringCollection.ToString();
        }

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
