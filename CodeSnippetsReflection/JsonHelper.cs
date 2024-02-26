using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace CodeSnippetsReflection
{
    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions;
        private static readonly JsonDocumentOptions _jsonDocumentOptions;

        static JsonHelper()
        {
            var encoderSettings = new TextEncoderSettings();
            encoderSettings.AllowCharacters('\u0027'); // apostrophe
            encoderSettings.AllowRange(UnicodeRanges.BasicLatin);

            _jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(encoderSettings),
            };

            _jsonDocumentOptions = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
        }

        public static JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions;

        public static JsonDocumentOptions JsonDocumentOptions => _jsonDocumentOptions;
    }
}
