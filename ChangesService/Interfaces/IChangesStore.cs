// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using System.Threading.Tasks;

namespace ChangesService.Interfaces
{
    /// <summary>
    /// Defines a contract for fetching changelog data.
    /// </summary>
    public interface IChangesStore
    {
        Task<ChangeLogList> FetchChangeLogListAsync(string locale);
    }
}
