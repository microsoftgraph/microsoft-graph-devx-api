using System.Collections.Generic;

namespace GraphExplorerSamplesService.Models
{
    /// <summary>
    /// Contains the list of sample queries categories defined in hierarchical order.
    /// </summary>
    internal class SampleQueriesCategories
    {
        private static readonly string[] categories =
           {
                // Order is important
                "Getting Started",
                "Users",
                "Groups",
                "Outlook Mail",
                "Outlook Mail (beta)",
                "Outlook Calendar",
                "Personal Contacts",
                "OneDrive",
                "Excel",
                "Planner",
                "Insights",
                "Insights (beta)",
                "People",
                "Extensions",
                "OneNote",
                "SharePoint Sites",
                "SharePoint Lists",
                "Batching",
                "Microsoft Teams",
                "Microsoft Teams (beta)",
                "Security",
                "User Activities",
                "Applications (beta)",
                "Notifications (beta)",
                "Search (beta)"
            };

        public static List<string> CategoriesList = new List<string>(categories);
    }
}
