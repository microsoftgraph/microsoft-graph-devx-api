// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------


using System;
using Newtonsoft.Json;
using System.Collections.Generic;

//to-do Add query to the data model
namespace TourStepsService.Models
{
    public class TourStepsModel
    {
        /*private fields*/
        private string _target;
        private string _content;
        private string _expectedActionType;
        private string _docLink;
        private string _title;
        private static QueryObject _queryObject;

        /*properties*/

        [JsonProperty(Required = Required.Always, PropertyName = "target")]
        public string Target
        {
            get => _target;
            set
            {
                _target = value.Trim(' '); //remove all leading and trailing white spaces
            }

        }

        [JsonProperty(Required = Required.Always, PropertyName = "content")]
        public string Content
        {
            get => _content;
            set
            {
                _content = value.Trim(' ');
            }
        }
        [JsonProperty("docLink")]
        public string DocLink
        {
            get => _docLink;
            set
            {
                if (value != "" && !Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    throw new ArgumentException("URL must be absolute and valid.", nameof(DocLink));
                }

                _docLink = value.Trim(' '); // remove all leading and trailing whitespaces before assigning
            }
        }
        [JsonProperty(Required = Required.Always, PropertyName = "directionalHint")]
        public int DirectionalHint
        {
            get; set;
        }

        [JsonProperty("spotligtClicks")]
        public bool SpotlightClicks
        {
            get; set;
        }

        [JsonProperty("autoNext")]
        public bool AutoNext
        {
            get; set;
        }

        [JsonProperty("disableBeacon")]
        public bool DisableBeacon
        {
            get; set;
        }

        [JsonProperty("advanced")]
        public bool Advanced
        {
            get; set;
        }

        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set
            {
                _title = value.Trim(' ');
            }
        }

        [JsonProperty("expectedActionType")]
        public string ExpectedActionType
        {
            get => _expectedActionType;
            set
            {
                _expectedActionType = value.Trim(' ');
            }
        }

        [JsonProperty("hideCloseButton")]
        public bool HideCloseButton
        {
            get;set;
        }

        [JsonProperty("query", NullValueHandling = NullValueHandling.Ignore)]
        public QueryObject Query
        {
            get => _queryObject;
            set
            {
                _queryObject = value;
            }
         
        }

        [JsonProperty("active")]
        public bool Active
        {
            get; set;
        }

        public class QueryObject
        {
            [JsonProperty("selectedVerb")]
            public string selectedVerb
            {
                get;
                set;
            }

            [JsonProperty("selectedVersion")]
            public string selectedVersion
            {
                get; set;
            }
            [JsonProperty("sampleUrl")]
            public string sampleUrl
            {
                get;set;
            }
            
            [JsonProperty("sampleHeaders", NullValueHandling = NullValueHandling.Ignore)]
            public Header[] sampleHeaders
            {
                get;
                set;
                
            }

            public class Header
            {
                [JsonProperty(PropertyName = "name")]
                public string Name
                {
                    get; set;
                }
                [JsonProperty(PropertyName = "value")]
                public string Value
                {
                    get; set;
                }
            }
        }

    }

    
}
