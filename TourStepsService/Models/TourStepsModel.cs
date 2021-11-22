// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace TourStepsService.Models
{
    public class TourStepsModel
    {
        /*private fields*/
        private string _target;
        private string _content;
        private string _expectedActionType;
        private string _docsLink;
        private string _title;

        /*properties*/

        [JsonProperty(Required = Required.Always, PropertyName = "target")]
        public string Target
        {
            get => _target;
            set
            {
                _target = value.Trim(' '); // remove all leading and trailing white spaces
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

        [JsonProperty(Required = Required.Always, PropertyName = "directionalHint")]
        public int DirectionalHint { get; set; }

        [JsonProperty("spotligtClicks")]
        public bool SpotlightClicks { get; set; }

        [JsonProperty("hideCloseButton")]
        public bool HideCloseButton { get; set; }

        [JsonProperty("autoNext")]
        public bool AutoNext { get; set; }

        [JsonProperty("disableBeacon")]
        public bool DisableBeacon { get; set; }

        [JsonProperty("advanced")]
        public bool Advanced { get; set; }

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

        [JsonProperty("docsLink")]
        public string DocsLink
        {
            get => _docsLink;
            set
            {
                _docsLink = value.Trim(' '); // remove all leading and trailing whitespaces before assigning

                if (!string.IsNullOrEmpty(_docsLink) && !Uri.IsWellFormedUriString(_docsLink, UriKind.Absolute))
                {
                    throw new ArgumentException("URL must be absolute and valid.", nameof(DocsLink));
                }
            }
        }

        [JsonProperty("query", NullValueHandling = NullValueHandling.Ignore)]
        public QueryObject Query { get; set; } = new();

        [JsonProperty("active")]
        public bool Active { get; set; }

        public class QueryObject
        {

            [JsonProperty("selectedVerb")]
            [JsonConverter(typeof(StringEnumConverter))]
            public HttpMethods SelectedVerb { get; set; }

            [JsonProperty("selectedVersion")]
            public string SelectedVersion { get; set; }

            [JsonProperty("sampleUrl")]
            public string SampleUrl { get; set; }

            [JsonProperty("sampleHeaders", NullValueHandling = NullValueHandling.Ignore)]
            public Header[] SampleHeaders { get; set; }

            public class Header
            {
                [JsonProperty(PropertyName = "name")]
                public string Name { get; set; }

                [JsonProperty(PropertyName = "value")]
                public string Value  { get; set; }
            }

            public enum HttpMethods
            {
                GET,
                PUT,
                POST,
                PATCH,
                DELETE
            }
        }
    }
}
