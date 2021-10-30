using System;
using System.Collections.Generic;
using System.Linq;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedExpressionsBuilder
    {
        private readonly string _tableAlias;
        private readonly List<Expression.Qualified> _slots = new();

        public IReadOnlyList<Expression> Slots => _slots;

        public SlottedExpressionsBuilder(FromClause from)
        {
            _tableAlias = GetTableAlias(from);
        }
        
        public Expression Build(Expression expr)
        {
            if (expr is Expression.Identifier identifier) return BuildQualified(new Expression.Qualified(identifier));
            if (expr is Expression.Qualified qualified) return BuildQualified(qualified);

            if (expr is Expression.Binary binary) return BuildBinary(binary);
            if (expr is Expression.Presence presence) return BuildPresence(presence);
            if (expr is Expression.In @in) return BuildIn(@in);
            
            if (expr is Expression.BooleanLiteral or Expression.NumberLiteral or Expression.StringLiteral) return expr;
            
            throw new NotImplementedException($"handle complex expressions: {expr.GetType().FullName}");
        }
        
        private Expression BuildQualified(Expression.Qualified qualified)
        {
            // TODO handle de-duplication

            if (qualified.Identifiers.Count == 1 && qualified.Identifiers[0].Name == "*")
            {
                _slots.Add(qualified);
                return new SlottedExpression(_slots.Count - 1);
            }

            if (!IsTableIdentifier(qualified.Identifiers[0]))
            {
                throw new NotImplementedException("handle non-table rooted qualified expressions");
            }
            
            _slots.Add(new Expression.Qualified(qualified.Identifiers.Skip(1).ToArray()));
            return new SlottedExpression(_slots.Count - 1);
        }
        
        private Expression BuildBinary(Expression.Binary binary) => 
            new Expression.Binary(binary.Operator, Build(binary.Left), Build(binary.Right));

        private Expression BuildPresence(Expression.Presence presence)
        {
            var expr = Build(presence.Expression);

            return new Expression.Presence(expr, presence.Negate);
        }
        
        private Expression BuildIn(Expression.In @in)
        {
            var expr = Build(@in.Expression);
            var matches = @in.Matches.Select(Build).ToArray();
            
            return new Expression.In(expr, matches);
        }
        
        private static string GetTableAlias(FromClause from)
        {
            if (!string.Equals(from.Table, "S3Object", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotImplementedException("complex table targets are not currently supported");
            }
            
            return from.Alias.Match(x => x, _ => "s3object");
        }
        
        private bool IsTableIdentifier(Expression.Identifier identifier) =>
            string.Equals(_tableAlias, identifier.Name, identifier.CaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase);
    }
}