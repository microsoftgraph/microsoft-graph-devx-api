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
using System.Threading.Tasks;
using UtilityService;

namespace KnownIssuesService.Services
{
    public class KnownIssuesService : IKnownIssuesService
    {
		/// <summary>
		/// Contain the Azure DevOps Known Issues Url
		/// </summary>
		private readonly string knownIssuesUri = "https://microsoftgraph.visualstudio.com/";
		private readonly IConfiguration _configuration;
		private readonly string _accessToken;

        public KnownIssuesService(IConfiguration configuration)
        {
            _configuration = configuration;
			_accessToken = _configuration["KnownIssuesToken"];
        }

        /// <summary>
        /// Function to Query the List of Known Issues from Azure DevOps Known Organization
        /// </summary>
        /// <returns>Known Issues Contract that contains json items that will be rendered on the browser</returns>
        public async Task<List<KnownIssuesContract>> QueryBugs()
		{

			List<KnownIssuesContract> knownIssuesList = new List<KnownIssuesContract>();

			VssBasicCredential credentials = new VssBasicCredential(string.Empty, _accessToken);

			// create a wiql object and build our query
			var wiql = new Wiql()
			{
				Query = "Select [Id] " +
						"From WorkItems " +
						"Where [Work Item Type] = 'Bug' " +
						"And [System.TeamProject] = '" + UtilityConstants.knownIssuesOrganisation + "' " +
						"Order By [State] Asc, [Changed Date] Desc",
			};

			try
			{
				WorkItemTrackingHttpClient httpClient = new WorkItemTrackingHttpClient(new Uri(knownIssuesUri), credentials);
				var result = await httpClient.QueryByWiqlAsync(wiql, null, 100).ConfigureAwait(false);
				var ids = result.WorkItems.Select(item => item.Id).ToArray();

				if (ids.Length == 0)
				{
					return new List<KnownIssuesContract>();
				}


				// build a list of the fields we want to see
				//var fields = new[] { "System.Id", "System.Title", "System.State", "System.Tags" };

				// get work items for the ids found in query
				var items = await httpClient.GetWorkItemsAsync(ids, null, result.AsOf).ConfigureAwait(false);

				foreach (var item in items)
				{
					KnownIssuesContract contract = new KnownIssuesContract();
					contract.Id = item?.Id;

					if (item.Fields.ContainsKey("System.State"))
					{
						contract.State = item.Fields["System.State"].ToString();
					}

					if (item.Fields.ContainsKey("System.Title"))
					{
						contract.Title = item.Fields["System.Title"].ToString();
					}

					if (item.Fields.ContainsKey("Custom.MSGraphM365Workload"))
					{
						contract.WorkLoadArea = item.Fields["Custom.MSGraphM365Workload"].ToString();
					}

					if (item.Fields.ContainsKey("System.Description"))
					{
						contract.Description = item.Fields["System.Description"].ToString();
					}

					if (item.Fields.ContainsKey("Custom.Workaround"))
					{
						contract.WorkAround = item.Fields["Custom.Workaround"].ToString();
					}

					if (item.Fields.ContainsKey("Custom.APIPathLink"))
					{
						contract.Link = item.Fields["Custom.APIPathLink"].ToString();
					}
					knownIssuesList.Add(contract);
				}

				return knownIssuesList;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
	}
}
