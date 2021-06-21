// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using ChangesService.Models;
using FileService.Interfaces;

namespace ChangesService.Interfaces
{
    public interface IChangesService
    {
        ChangeLogRecords DeserializeChangeLogRecords(string jsonString);

        ChangeLogRecords FilterChangeLogRecords(ChangeLogRecords changeLogRecords,
                                                ChangeLogSearchOptions searchOptions,
                                                MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                IHttpClientUtility httpClientUtility = null);
    }
}
