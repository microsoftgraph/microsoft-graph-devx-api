// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace ChangesService.Models
{
    /// <summary>
    /// Defines the structure of a changelog model.
    /// </summary>
    public class ChangeLog
    {
        public List<Change> ChangeList { get; set; }
        public string Id { get; set; }
        public string Cloud { get; set; }
        public string Version { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public string WorkloadArea { get; set; }
        public string SubArea { get; set; }

        /// <summary>
        /// Defines the structure of a single change model.
        /// </summary>
        public class Change
        {
            public string Id { get; set; }
            public string ApiChange { get; set; }
            public string ChangedApiName { get; set; }
            public string ChangeType { get; set; }
            public string Description { get; set; }
            public string Target { get; set; }
        }
    }
}
