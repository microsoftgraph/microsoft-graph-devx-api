// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Interfaces;
using KnownIssuesService.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UtilityService;

namespace KnownIssuesService.Services
{
    /// <summary>
    /// Fetches a list of known issues from Azure Devops
    /// </summary>
    public class KnownIssuesService : IKnownIssuesService
    {
		private List<KnownIssue> _knownIssuesList;
		private readonly Wiql _workItemQuery;
		private readonly WorkItemTrackingHttpClient _httpQueryClient;
		private readonly IConfiguration _configuration;
		private readonly TelemetryClient _telemetryClient;
		private readonly Dictionary<string, string> _knownIssuesTraceProperties =
			new() { { UtilityConstants.TelemetryPropertyKey_KnownIssues, nameof(KnownIssuesService) } };
		private const string AccessTokenValue = "KnownIssues:Token";
		private const string KnownIssuesPath = "KnownIssues:Uri";


		public KnownIssuesService(IConfiguration configuration,
								  TelemetryClient telemetryClient = null,
								  Wiql wiql = null,
								  WorkItemTrackingHttpClient httpQueryClient = null)
		{
			UtilityFunctions.CheckArgumentNull(configuration, nameof(configuration));
			_configuration = configuration;
			_telemetryClient = telemetryClient;
			_workItemQuery = wiql ?? QueryBuilder();
			_httpQueryClient = httpQueryClient ?? GetWorkItemTrackingHttpClient();
		}

        /// <summary>
        /// Authenticates a process/service to a Visual Studio Service.
        /// </summary>
        /// <returns>An instance of the authenticated VS Service</returns>
        private VssBasicCredential Authenticate()
		{
			_telemetryClient?.TrackTrace("Fetch personal access token from configuration and use it to authenticate into Visual Studio Service",
										 SeverityLevel.Information,
										 _knownIssuesTraceProperties);

			var accessToken = _configuration[AccessTokenValue];
			return new VssBasicCredential(string.Empty, accessToken);
		}

		/// <summary>
		/// Initializes a WorkItemTracking Http Client instance with the required parameters i.e. credentials
		/// and the Azure DevOps Known Issues Organisation url
		/// </summary>
		/// <returns>An Instance of a WorkItemTrackingHttpClient</returns>
		private WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
		{
            try
            {
				_telemetryClient?.TrackTrace("Initialize a WorkItemTrackingHttpClient instance with the required credentials",
														 SeverityLevel.Information,
														 _knownIssuesTraceProperties);

				var knownIssuesUri = _configuration[KnownIssuesPath];
				return new WorkItemTrackingHttpClient(new Uri(knownIssuesUri), Authenticate());
			}
            catch
            {
				throw;
            }
		}

		/// <summary>
		/// Creates a WorkItemQuery Result used for getting work item ids and their urls from
		/// Azure DevOps Org
		/// </summary>
		/// <returns>WorkItem Query result</returns>
		public Task<WorkItemQueryResult> GetQueryByWiqlAsync()
		{
			return _httpQueryClient.QueryByWiqlAsync(_workItemQuery, null, 100, null, CancellationToken.None);
		}

		/// <summary>
		/// Retrieves WorkItems data from Azure DevOps Org based on the passed in work item ids
		/// </summary>
		/// <param name="ids">Work Item Ids From Azure DevOps Organisation</param>
		/// <param name="result">WorkItemQuery result that contains the specific ids and their urls</param>
		/// <returns>A List of Work Items from Azure DevOps Org</returns>
		public Task<List<WorkItem>> GetWorkItemsQueryAsync(int[] ids, WorkItemQueryResult result)
		{
			_telemetryClient?.TrackTrace($"Fetch WorkItems query result {result} based on {ids}",
										 SeverityLevel.Information,
										 _knownIssuesTraceProperties);

			return _httpQueryClient.GetWorkItemsAsync(ids, null, result.AsOf, null, null, null, CancellationToken.None);
		}

		/// <summary>
		/// Create a Query builder for retrieving work items from an Azure DevOps Organisation
		/// </summary>
		/// <returns>A work item query builder containing the selection criteria</returns>
		private static Wiql QueryBuilder()
		{
			// create a wiql object and build our query
			return new Wiql()
			{
				Query = "Select [Id] " +
						"From WorkItems " +
						"Where [Work Item Type] = 'Bug' " +
						"And [System.TeamProject] = '" + UtilityConstants.KnownIssuesOrganisation + "' " +
						"Order By [State] Asc, [Changed Date] Desc",
			};
		}

		/// <summary>
		/// Function to Query the List of Known Issues from Azure DevOps Known Organization
		/// </summary>
		/// <returns>Known Issues Contract that contains json items that will be rendered on the browser</returns>
		public async Task<List<KnownIssue>> QueryBugsAsync()
		{
			_telemetryClient?.TrackTrace("Fetches a WorkItemQueryResult for fetching work item Ids and urls",
										 SeverityLevel.Information,
										 _knownIssuesTraceProperties);

			WorkItemQueryResult result = await GetQueryByWiqlAsync();
			int[] ids = result.WorkItems.Select(item => item.Id).ToArray();

            if (ids.Length == 0)
            {
                return _knownIssuesList;
            }

			_telemetryClient?.TrackTrace("Use the selected ids to fetch their subsequent work items from the query result",
										 SeverityLevel.Information,
										 _knownIssuesTraceProperties);

			// get work items for the ids found in query
			List<WorkItem> items = await GetWorkItemsQueryAsync(ids, result);

			_knownIssuesList = items.Where(x => x!=null).Select(x => new KnownIssue
			{
				Id = x.Id,
				State = x.Fields.TryGetValue("System.State", out var state) ? state.ToString(): default,
				Title = x.Fields.TryGetValue("System.Title", out var title) ? title.ToString() : default,
				WorkLoadArea = x.Fields.TryGetValue("Custom.MSGraphM365Workload", out var workLoadArea) ? workLoadArea.ToString() : default,
				Description = x.Fields.TryGetValue("System.Description", out var description) ? description.ToString() : default,
				WorkAround = x.Fields.TryGetValue("Custom.Workaround", out var workAround) ? workAround.ToString() : default,
				Link = x.Fields.TryGetValue("Custom.APIPathLink", out var link) ? link.ToString() : default
			}).ToList();

			_telemetryClient?.TrackTrace("Return a list of Known Issues",
										 SeverityLevel.Information,
										 _knownIssuesTraceProperties);

			return _knownIssuesList;
		}
	}
}
