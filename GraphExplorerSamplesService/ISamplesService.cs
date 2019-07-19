
namespace GraphExplorerSamplesService
{
    public interface ISamplesService
    {
        SampleQueriesList ReadFromJsonFile(string filePathName);
    }
}
