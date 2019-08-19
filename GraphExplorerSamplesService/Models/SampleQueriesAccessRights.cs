using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Defines a list which holds a collection of access rights for the list of sample queries.
    /// </summary>
    public class SampleQueriesAccessRights
    {
        /// <summary>
        /// A list of <see cref="CategoryRights"/> objects.
        /// </summary>
        public List<CategoryRights> AccessRights { get; set; }
    }
}
