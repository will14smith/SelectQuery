using System;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Trees;

public class QueryTreeCompiler
{
    public QueryTreePlan Build(Query query)
    {
        if (query.Where.IsSome)
        {
            // build where tree
            // record predicates
        }
        
        // build projection tree (re-using where tree)
        // record projections
        
        throw new NotImplementedException();
    } 
}