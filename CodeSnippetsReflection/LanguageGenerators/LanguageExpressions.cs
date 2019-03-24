namespace CodeSnippetsReflection.LanguageGenerators
{
    public abstract class LanguageExpressions
    {
        public abstract string FilterExpression { get;  }
        public abstract string FilterExpressionDelimiter { get; }
        public abstract string ExpandExpression { get;  }
        public abstract string ExpandExpressionDelimiter { get; }
        public abstract string SelectExpression { get;  }
        public abstract string SelectExpressionDelimiter { get; }
        public abstract string OrderByExpression { get;  }
        public abstract string OrderByExpressionDelimiter { get; }
        public abstract string SkipExpression { get;  }
        public abstract string SkipTokenExpression { get;  }
        public abstract string TopExpression { get;  }
        public abstract string SearchExpression { get; }
        public abstract string HeaderExpression { get; }

    }
}
