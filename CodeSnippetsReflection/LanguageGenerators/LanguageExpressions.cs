using System;
using System.Collections.Generic;
using System.Text;

namespace CodeSnippetsReflection
{
    public abstract class LanguageExpressions
    {
        abstract public string FilterExpression { get;  }
        abstract public string FilterExpressionDelimiter { get; }
        abstract public string ExpandExpression { get;  }
        abstract public string ExpandExpressionDelimiter { get; }
        abstract public string SelectExpression { get;  }
        abstract public string SelectExpressionDelimiter { get; }
        abstract public string OrderByExpression { get;  }
        abstract public string OrderByExpressionDelimiter { get; }
        abstract public string SkipExpression { get;  }
        abstract public string SkipTokenExpression { get;  }
        abstract public string TopExpression { get;  }
        abstract public string SearchExpression { get; }

    }
}
