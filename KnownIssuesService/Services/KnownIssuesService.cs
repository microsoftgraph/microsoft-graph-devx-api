// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using KnownIssuesService.Interfaces;
using KnownIssuesService.Models;
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
		private readonly WorkItemTrackingHttpClient _httpQueryClient;
		private readonly Wiql _workItemQuery;

        private readonly IConfiguration _configuration;
        private const string AccessTokenValue = "KnownIssues:Token";
		private const string KnownIssuesPath = "KnownIssues:Uri";
		private List<KnownIssue> KnownIssuesList { get; set; }

		public KnownIssuesService(IConfiguration configuration)
		{
			UtilityFunctions.CheckArgumentNull(configuration, nameof(configuration));
			_configuration = configuration;
			_workItemQuery = QueryBuilder();
			_httpQueryClient = GetWorkItemTrackingHttpClient();
		}

        public KnownIssuesService(WorkItemTrackingHttpClient httpQueryClient, Wiql workItemQuery)
        {
            _httpQueryClient = httpQueryClient;
            _workItemQuery = workItemQuery;
        }

        /// <summary>
        /// Authenticates a process/service to a Visual Studio Service.
        /// </summary>
        /// <returns>An instance of the authenticated VS Service</returns>
        public VssBasicCredential Authenticate()
		{
			var accessToken = _configuration[AccessTokenValue];
			return new VssBasicCredential(string.Empty, accessToken); ;
		}

		/// <summary>
		/// Initializes a WorkItemTracking Http Client instance with the required parameters i.e. credentials
		/// and the Azure DevOps Known Issues Organisation url
		/// </summary>
		/// <returns>An Instance of a WorkItemTrackingHttpClient</returns>
		public WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
		{
			var knownIssuesUri = _configuration[KnownIssuesPath];
			return new WorkItemTrackingHttpClient(new Uri(knownIssuesUri), Authenticate());
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
			return _httpQueryClient.GetWorkItemsAsync(ids, null, result.AsOf, null, null, null, CancellationToken.None);
		}

		/// <summary>
		/// Create a Query builder for retrieving work items from an Azure DevOps Organisation
		/// </summary>
		/// <returns>A work item query builder containing the selection criteria</returns>
		public static Wiql QueryBuilder()
		{
			// create a wiql object and build our query
			return new Wiql()
			{
				Query = "Select [Id] " +
						"From WorkItems " +
						"Where [Work Item Type] = 'Bug' " +
						"And [System.TeamProject] = '" + UtilityConstants.knownIssuesOrganisation + "' " +
						"Order By [State] Asc, [Changed Date] Desc",
			};
		}

		/// <summary>
		/// Function to Query the List of Known Issues from Azure DevOps Known Organization
		/// </summary>
		/// <returns>Known Issues Contract that contains json items that will be rendered on the browser</returns>
		public async Task<List<KnownIssue>> QueryBugsAsync()
		{
			try
			{
				WorkItemQueryResult result = await GetQueryByWiqlAsync();
				var ids = result.WorkItems.Select(item => item.Id).ToArray();

                if (ids.Length == 0)
                {
                    return KnownIssuesList;
                }

                // get work items for the ids found in query
                List<WorkItem> items = await GetWorkItemsQueryAsync(ids, result);

				KnownIssuesList = items.Select(x => new KnownIssue
				{
					Id = x?.Id,
					State = x.Fields.TryGetValue("System.State", out var state) ? state.ToString(): default,
					Title = x.Fields.TryGetValue("System.Title", out var title) ? title.ToString() : default,
					WorkLoadArea = x.Fields.TryGetValue("Custom.MSGraphM365Workload", out var workLoadArea) ? workLoadArea.ToString() : default,
					Description = x.Fields.TryGetValue("System.Description", out var description) ? description.ToString() : default,
					WorkAround = x.Fields.TryGetValue("Custom.Workaround", out var workAround) ? workAround.ToString() : default,
					Link = x.Fields.TryGetValue("Custom.APIPathLink", out var link) ? link.ToString() : default
				}).ToList();

				return KnownIssuesList;
			}
            catch
			{
				throw;
			}
		}
	}
}
