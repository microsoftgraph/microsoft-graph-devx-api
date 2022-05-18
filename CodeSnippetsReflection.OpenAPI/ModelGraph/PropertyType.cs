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
        String ,
        Number,
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
