using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GraphExplorerSamplesService
{
    public interface IFileUtility
    {
        Task<string> ReadFromFile(string filePathSource);
    }
}
