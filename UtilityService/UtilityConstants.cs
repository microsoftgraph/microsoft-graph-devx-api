// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using System.Collections.Generic;

namespace UtilityService
{
    /// <summary>
    /// Provides commonly used reusable constants.
    /// </summary>
    public static class UtilityConstants
    {
        public const string TelemetryPropertyKey_SanitizeIgnore = "SanitizeIgnore";
        public const string TelemetryPropertyKey_Permissions = "Permissions";
        public const string TelemetryPropertyKey_Samples = "Samples";
        public const string TelemetryPropertyKey_Changes = "Changes";
        public const string TelemetryPropertyKey_OpenApi = "OpenApi";
        public const string TelemetryPropertyKey_Snippets = "Snippets";
        public const string TelemetryPropertyKey_KnownIssues = "KnownIssues";
        public const string TelemetryPropertyKey_TourSteps = "TourSteps";

        public const string ServiceRootV1 = "https://graph.microsoft.com/v1.0";
        public const string ServiceRootBeta = "https://graph.microsoft.com/beta";
        public const string CleanV1Metadata = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_v10_metadata/cleanMetadataWithDescriptionsv1.0.xml";
        public const string CleanBetaMetadata = "https://raw.githubusercontent.com/microsoftgraph/msgraph-metadata/master/clean_beta_metadata/cleanMetadataWithDescriptionsbeta.xml";

        /// <summary>
        /// Contains the name of the Known Issues Azure DevOps Organisation
		/// </summary>
		public const string KnownIssuesOrganisation = "Known Issues (staging)";

        public const string NullValueError = "Value cannot be null";
    }
}
