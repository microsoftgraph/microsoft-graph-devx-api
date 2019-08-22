using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a list which holds a collection of <see cref="CategoryPolicy"/> objects.
    /// </summary>
    public class SampleQueriesPolicies
    {
        /// <summary>
        /// A list of <see cref="CategoryPolicy"/> objects.
        /// </summary>
        public List<CategoryPolicy> CategoryPolicies { get; set; }

        public SampleQueriesPolicies()
        {
            CategoryPolicies = new List<CategoryPolicy>();
        }
    }
}
