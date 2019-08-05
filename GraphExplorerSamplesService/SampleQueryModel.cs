using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace GraphExplorerSamplesService
{
    public class SampleQueryModel
    {
        // Field data for the custom mutator methods
        private string _humanName;
        private string _docLink;
        private string _category;
        private readonly LinkedList<string> _categories;

        public SampleQueryModel()
        {
            _categories = new LinkedList<string>();
            Categories = _categories;
        }


        /* Properties */

        [JsonProperty("id")]
        public Guid Id { get; set; }
                
        [JsonProperty(Required = Required.Always, PropertyName = "category")]
        public string Category
        {
            get => _category;
            set
            {
                if (!Categories.Contains(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(Category), "The category specified does not exist in the list of categories:\r\n" + BuildStringOfCategories());
                }

                _category = value;
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

                _humanName = value;
            }
        }

        [JsonProperty(Required = Required.Always, PropertyName = "requestUrl")]
        public string RequestUrl { get; set; }
                
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

                _docLink = value;
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

        [JsonIgnore]
        public LinkedList<string> Categories
        {
            get => _categories;
            private set
            {
                // The order is important
                _categories.AddLast("Getting Started");
                _categories.AddLast("Users");
                _categories.AddLast("Groups");
                _categories.AddLast("Outlook Mail");
                _categories.AddLast("Outlook Mail (beta)");
                _categories.AddLast("Outlook Calendar");
                _categories.AddLast("Personal Contacts");
                _categories.AddLast("OneDrive");
                _categories.AddLast("Excel");
                _categories.AddLast("Planner");
                _categories.AddLast("Insights");
                _categories.AddLast("Insights (beta)");
                _categories.AddLast("People");
                _categories.AddLast("Extensions");
                _categories.AddLast("OneNote");
                _categories.AddLast("SharePoint Sites");
                _categories.AddLast("SharePoint Lists");
                _categories.AddLast("Batching");
                _categories.AddLast("Microsoft Teams");
                _categories.AddLast("Microsoft Teams (beta)");
                _categories.AddLast("Security");
                _categories.AddLast("User Activities");
                _categories.AddLast("Applications (beta)");
                _categories.AddLast("Notifications (beta)");
            }
        }

        public string BuildStringOfCategories()
        {
            StringBuilder categoriesStringCollection = new StringBuilder();
          
            foreach (string category in Categories)
            {
                categoriesStringCollection.Append(category + "\r\n");
            }

            return categoriesStringCollection.ToString();
        }

        
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
