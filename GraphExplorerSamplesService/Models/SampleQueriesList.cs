using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a list which holds a collection of <see cref="SampleQueryModel"/> objects.
    /// </summary>
    public class SampleQueriesList
    {
        /// <summary>
        /// A list of <see cref="SampleQueryModel"/> objects
        /// </summary>
        public List<SampleQueryModel> SampleQueries { get; set; }

        public SampleQueriesList(List<SampleQueryModel> sampleQueries)
        {
            SampleQueries = sampleQueries;
        }
    }
}
