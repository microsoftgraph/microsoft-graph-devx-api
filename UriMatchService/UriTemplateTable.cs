using System;
using System.Collections.Generic;

namespace UriMatchService
{
    public class UriTemplateTable
    {
        private readonly Dictionary<string, UriTemplate> _templates = new Dictionary<string, UriTemplate>();

        public void Add(string key, UriTemplate template)
        {
            _templates.Add(key, template);
        }

        public TemplateMatch Match(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url), "Value cannot be null.");
            }

            Uri absolutePath = url;
            if (url.IsAbsoluteUri)
            {
                absolutePath = new Uri(url.AbsolutePath, UriKind.Relative);
            }

            foreach (var template in _templates)
            {
                var parameters = template.Value.GetParameters(absolutePath);
                if (parameters != null)
                {
                    return new TemplateMatch() { Key = template.Key, Template = template.Value };
                }
            }
            return null;
        }

        public UriTemplate this[string key]
        {
            get
            {
                if (_templates.TryGetValue(key, out UriTemplate value))
                {
                    return value;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public class TemplateMatch
    {
        public string Key { get; set; }
        public UriTemplate Template { get; set; }
    }
}

