// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace SamplesService.Models
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

        public SampleQueriesList()
        {
            SampleQueries = new List<SampleQueryModel>();
        }
    }
}
