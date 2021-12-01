// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using ChangesService.Models;
using FileService.Interfaces;

namespace ChangesService.Interfaces
{
    public interface IChangesService
    {
        ChangeLogRecords DeserializeChangeLogRecords(string jsonString);

        Task<ChangeLogRecords> FilterChangeLogRecordsByUrlAsync(string requestUrl,
                                                                ChangeLogRecords changeLogRecords,
                                                                MicrosoftGraphProxyConfigs graphProxyConfigs,
                                                                Dictionary<string, string> workloadServiceMappings,
                                                                IHttpClientUtility httpClientUtility);
        (string GraphVersion, string RequestUrl) ExtractGraphVersionAndUrlValues(string requestUrl);
    }
}
