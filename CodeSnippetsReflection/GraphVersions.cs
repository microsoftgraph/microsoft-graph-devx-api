using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeSnippetsReflection
{
    public static class GraphMetadataContainer
    {
        public static IEdmModel graphMetadataVersion1 { get; set; }
        public static IEdmModel graphMetadataVersionBeta { get; set; }
    }
}
