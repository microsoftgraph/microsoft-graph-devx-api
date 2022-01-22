// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace TourStepsService.Models
{
    /// <summary>
    /// Defines a list which holds a collection of <see cref="TourStepsModel"/> objects.
    /// </summary>
    public class TourStepsList
    {
        /// <summary>
        /// A list of <see cref="TourStepsModel"/> objects
        /// </summary>
        public List<TourStepsModel> TourSteps { get; set; } = new();
    }
}
