using System;

namespace SelectQuery.Evaluation;

[Serializable]
public class QueryValidationError : Exception
{
    public QueryValidationError(string message) : base(message) { }
}