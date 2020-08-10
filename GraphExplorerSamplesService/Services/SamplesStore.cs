// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using FileService.Common;
using FileService.Interfaces;
using GraphExplorerSamplesService.Interfaces;
using GraphExplorerSamplesService.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace GraphExplorerSamplesService.Services
{
    /// <summary>
    /// Defines a method that retrieves or adds localized sample queries to and from a memory cache or a remote source.
    /// </summary>
    public class SamplesStore : ISamplesStore
    {
        private readonly object _samplesLock = new object();
        private readonly IFileUtility _fileUtility;
        private readonly IMemoryCache _samplesCache;
        private readonly IConfiguration _configuration;
        private readonly string _sampleQueriesContainerName;
        private readonly string _sampleQueriesBlobName;
        private readonly int _defaultRefreshTimeInHours;

        public SamplesStore(IFileUtility fileUtility, IConfiguration configuration, IMemoryCache samplesCache)
        {
            _fileUtility = fileUtility;
            _samplesCache = samplesCache;
            _configuration = configuration;
            _sampleQueriesContainerName = _configuration["AzureBlobStorage:Containers:SampleQueries"];
            _sampleQueriesBlobName = _configuration[$"AzureBlobStorage:Blobs:SampleQueries"];
            _defaultRefreshTimeInHours = FileServiceHelper.GetFileCacheRefreshTime(configuration["FileCacheRefreshTimeInHours"]);
        }

        /// <summary>
        /// Fetches the sample queries from the cache or a JSON file and returns a deserialized instance of a
        /// <see cref="SampleQueriesList"/> from this.
        /// </summary>
        /// <param name="locale">The language code for the preferred localized file.</param>
        /// <returns>The deserialized instance of a <see cref="SampleQueriesList"/>.</returns>
        public async Task<SampleQueriesList> FetchSampleQueriesListAsync(string locale)
        {
            // Fetch cached sample queries
            SampleQueriesList sampleQueriesList = await _samplesCache.GetOrCreateAsync(locale, async cacheEntry =>
            {
                // Localized copy of samples is to be seeded by only one executing thread.
                lock (_samplesLock)
                {
                    /* Check whether a previous thread already seeded an
                     * instance of the localized samples during the lock.
                     */
                    var lockedLocale = locale;
                    var seededSampleQueriesList = _samplesCache.Get<SampleQueriesList>(lockedLocale);

                    if (seededSampleQueriesList != null)
                    {
                        return seededSampleQueriesList;
                    }

                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_defaultRefreshTimeInHours);

                    // Fetch the requisite sample path source based on the locale
                    string queriesFilePathSource =
                           FileServiceHelper.GetLocalizedFilePathSource(_sampleQueriesContainerName, _sampleQueriesBlobName, lockedLocale);

                    // Get the file contents from source
                    string jsonFileContents = _fileUtility.ReadFromFile(queriesFilePathSource).GetAwaiter().GetResult();

                    /* Current business process only supports ordering of the English
                       translation of the sample queries.
                     */
                    bool orderSamples = lockedLocale.Equals("en-us", StringComparison.OrdinalIgnoreCase);

                    // Return the list of the sample queries from the file contents
                    return SamplesService.DeserializeSampleQueriesList(jsonFileContents, orderSamples);
                }
            });

            return sampleQueriesList;
        }
    }
}
