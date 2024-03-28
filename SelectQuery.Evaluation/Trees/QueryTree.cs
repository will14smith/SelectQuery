using System.Collections.Generic;

namespace SelectQuery.Evaluation.Trees;

public class QueryTree
{
    private readonly IReadOnlyDictionary<string, QueryTree> _caseSensitiveChildren;
    private readonly IReadOnlyDictionary<string, QueryTree> _caseInsensitiveChildren;

    public int Index { get; }
    public QueryTreeOutputType Output { get; }
    
    public bool HasChildren => _caseSensitiveChildren.Count > 0 || _caseInsensitiveChildren.Count > 0;
}