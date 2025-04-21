using System;
using SelectParser.Queries;
using SimdJsonDotNet.Model;

namespace SelectQuery.Evaluation;

internal abstract class PredicateEvaluator : IDisposable
{
    public static PredicateEvaluator Create(Query query)
    {
        if (query.Where.IsNone)
        {
            return new AlwaysTrue();
        }
        
        return new EvaluateExpression(query, query.Where.Value!.Condition);
    }
    
    public abstract bool Test(DocumentReference record);
    public virtual void Dispose() { }

    private class AlwaysTrue : PredicateEvaluator
    {
        public override bool Test(DocumentReference record)
        {
            return true;
        }
    }
    
    private class EvaluateExpression(Query query, Expression predicate) : PredicateEvaluator
    {
        public override bool Test(DocumentReference record)
        {
            var tableAlias = query.From.Alias.Match(alias => alias, _ => "s3object");
            var value = ValueEvaluator.Evaluate(record, predicate, tableAlias);

            return value.AsBoolean();
        }
    }
}