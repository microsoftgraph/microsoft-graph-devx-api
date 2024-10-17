using System.Net.Http;
using System.Threading.Tasks;

namespace CodeSnippetsReflection
{
    public interface ISnippetsGenerator
    {
        Task<string> ProcessPayloadRequestAsync(HttpRequestMessage requestPayload, string language);
    }
}
