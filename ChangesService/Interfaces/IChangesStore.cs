// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System.Globalization;
using System.Threading.Tasks;

namespace ChangesService.Interfaces
{
    /// <summary>
    /// A contract for fetching changelog data.
    /// </summary>
    public interface IChangesStore
    {
        Task<ChangeLogRecords> FetchChangeLogRecordsAsync(CultureInfo cultureInfo);
    }
}
