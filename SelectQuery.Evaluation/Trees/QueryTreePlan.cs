using System.Collections.Generic;
using System.Text.Json;

namespace SelectQuery.Evaluation.Trees;

public class QueryTreePlan
{
    public IReadOnlyCollection<QueryTreeExpression> Predicates { get; }
    public IReadOnlyCollection<(string, QueryTreeExpression)> Projections { get; }
    
    public QueryTree Tree { get; }

    public QueryTreePlan(IReadOnlyCollection<QueryTreeExpression> predicates, IReadOnlyCollection<(string, QueryTreeExpression)> projections, QueryTree tree)
    {
        Predicates = predicates;
        Projections = projections;
        Tree = tree;
    }

    public void Execute(ref JsonLinesReader reader, Utf8JsonWriter writer)
    {
        
        throw new System.NotImplementedException();
    }
}