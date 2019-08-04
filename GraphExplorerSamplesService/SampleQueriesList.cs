using System.Collections.Generic;

namespace GraphExplorerSamplesService
{

    public class SampleQueriesList
    {
        /// <summary>
        /// The list of sample query objects
        /// </summary>
        public List<SampleQueryModel> SampleQueries { get; set; }

        public SampleQueriesList(List<SampleQueryModel> sampleQueries)
        {
            SampleQueries = sampleQueries;
        }
    }
}
