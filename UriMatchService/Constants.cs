// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace UriMatchingService
{
    /// <summary>
    /// Defines constants for the UriMatchService library classes
    /// </summary>
    internal static class Constants
    {
        // Regex constants
        private const string VarName = "[a-zA-Z0-9_]*";
        private const string Op = "(?<op>[+#./;?&]?)";
        private const string Var = "(?<var>(?:(?<lvar>" + VarName + ")[*]?,?)*)";
        public const string VarSpec = "(?<varspec>{" + Op + Var + "})";

        // Message constants
        public const string ValueNullOrEmpty = "Value cannot be null or empty.";
        public const string ValueNull = "Value cannot be null";
    }
}
