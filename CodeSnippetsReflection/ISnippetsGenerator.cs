using System.Net.Http;

namespace CodeSnippetsReflection
{
    public interface ISnippetsGenerator
    {
        string ProcessPayloadRequest(HttpRequestMessage requestPayload, string lang);
    }
}