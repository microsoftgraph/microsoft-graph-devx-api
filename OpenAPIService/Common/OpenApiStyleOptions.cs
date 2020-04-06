// ------------------------------------------------------------------------------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------------------------------------------------------------------------------

namespace OpenAPIService.Common
{
    /// <summary>
    /// Defines a model class that defines the different styling options for rendering an OpenAPI document.
    /// </summary>
    public class OpenApiStyleOptions
    {
        public OpenApiStyle Style { get; }
        public string OpenApiVersion { get; private set; }
        public string GraphVersion { get; private set; }
        public string OpenApiFormat { get; private set; }
        public bool InlineLocalReferences { get; private set; } = false;
        public bool EnablePagination { get; private set; } = false;
        public bool EnableDiscriminatorValue { get; private set; } = false;
        public bool EnableDerivedTypesReferencesForRequestBody { get; private set; } = false;
        public bool EnableDerivedTypesReferencesForResponses { get; private set; } = false;

        public OpenApiStyleOptions(OpenApiStyle style, string openApiVersion = null, string graphVersion = null, string openApiFormat = null)
        {
            Style = style;
            OpenApiVersion = openApiVersion;
            GraphVersion = graphVersion;
            OpenApiFormat = openApiFormat;

            SetStyleOptions();
        }

        private void SetStyleOptions()
        {
            switch (Style)
            {
                case OpenApiStyle.Plain:
                    SetPlainStyle();
                    break;
                case OpenApiStyle.PowerPlatform:
                    SetPowerPlatformStyle();
                    break;
                case OpenApiStyle.PowerShell:
                    SetPowerShellStyle();
                    break;
                default:
                    break;
            }
        }

        private void SetPlainStyle()
        {
            OpenApiVersion = OpenApiVersion ?? Constants.OpenApiConstants.OpenApiVersion_2;
            GraphVersion = GraphVersion ?? Constants.OpenApiConstants.GraphVersion_V1;
            OpenApiFormat = OpenApiFormat ?? Constants.OpenApiConstants.Format_Yaml;
        }

        private void SetPowerPlatformStyle()
        {
            OpenApiVersion = OpenApiVersion ?? Constants.OpenApiConstants.OpenApiVersion_2;
            GraphVersion = GraphVersion ?? Constants.OpenApiConstants.GraphVersion_V1;
            OpenApiFormat = OpenApiFormat ?? Constants.OpenApiConstants.Format_Json;
            InlineLocalReferences = true;
        }

        private void SetPowerShellStyle()
        {
            OpenApiVersion = OpenApiVersion ?? Constants.OpenApiConstants.OpenApiVersion_3;
            GraphVersion = GraphVersion ?? Constants.OpenApiConstants.GraphVersion_V1;
            OpenApiFormat = OpenApiFormat ?? Constants.OpenApiConstants.Format_Yaml;
            EnablePagination = true;
            EnableDiscriminatorValue = true;
            EnableDerivedTypesReferencesForRequestBody = true;
            EnableDerivedTypesReferencesForResponses = true;
        }
    }
}
