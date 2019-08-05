using System.Threading.Tasks;

namespace GraphExplorerSamplesService
{
    public interface IFileUtility
    {
        Task<string> ReadFromFile(string filePathSource);
    }
}
