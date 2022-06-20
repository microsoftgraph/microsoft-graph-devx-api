using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSnippetsReflection.OpenAPI.ModelGraph
{
    public enum PropertyType
    {
        // Empty object
        Default,
        String,
        Guid,
        Int32,
        Int64,
        Float32,
        Float64,
        Double,
        Date ,
        Boolean ,
        Null,
        Enum ,
        Object,
        Base64Url,
        Binary,
        Array,
        Map
    }
}
