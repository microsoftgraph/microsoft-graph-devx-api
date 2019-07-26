using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GraphExplorerSamplesService
{
    public class DiskFileUtility : IFileUtility
    {
        /// <summary>
        /// Reads the contents of a provided file on disk.
        /// </summary>
        /// <param name="filePathSource">The directory path name of the file on disk.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown when part of the file or directory cannot be found.</exception>
        /// <exception cref="FileNotFoundException">Thrown when an attempt to access the file if it does not exist on disk fails.</exception>
        /// <exception cref="IOException">Thrown when any other I/O error occurs.</exception>
        /// <returns>The contents of the file.</returns>
        public async Task<string> ReadFromFile(string filePathSource)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(filePathSource))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch(DirectoryNotFoundException dirException)
            {
                throw dirException;
            }
            catch(FileNotFoundException fileException)
            {
                throw fileException;
            }
            catch (IOException ioException)
            {
                throw ioException;
            }
        }
    }
}
