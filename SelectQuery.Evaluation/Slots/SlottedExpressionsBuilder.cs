using System;
using System.Linq;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedExpressionsBuilder
    {
        private readonly string _tableAlias;
        private readonly SlottedExpressionTree.Builder _slotTree = new();

        public SlottedExpressionTree SlotTree => _slotTree.Build();
        public int NumberOfSlots { get; private set; }

        public SlottedExpressionsBuilder(FromClause from)
        {
            _tableAlias = GetTableAlias(from);
        }
        
        public Expression Build(Expression expr, bool passthrough)
        {
            if (expr is Expression.Identifier identifier) return BuildQualified(new Expression.Qualified(identifier), passthrough);
            if (expr is Expression.Qualified qualified) return BuildQualified(qualified, passthrough);

            if (expr is Expression.Binary binary) return BuildBinary(binary);
            if (expr is Expression.Presence presence) return BuildPresence(presence);
            if (expr is Expression.In @in) return BuildIn(@in);
            
            if (expr is Expression.BooleanLiteral or Expression.NumberLiteral or Expression.StringLiteral) return expr;
            
            throw new NotImplementedException($"handle complex expressions: {expr.GetType().FullName}");
        }
        
        private Expression BuildQualified(Expression.Qualified qualified, bool passthrough)
        {
            if (qualified.Identifiers.Count == 1 && qualified.Identifiers[0].Name == "*")
            {
                return new SlottedExpression(AllocateSlot(_slotTree.Root, passthrough));
            }

            if (!IsTableIdentifier(qualified.Identifiers[0]))
            {
                throw new NotImplementedException("handle non-table rooted qualified expressions");
            }

            var node = _slotTree.FindOrCreate(new Expression.Qualified(qualified.Identifiers.Skip(1).ToArray()));
            return new SlottedExpression(AllocateSlot(node, passthrough));
        }

        private int AllocateSlot(SlottedExpressionTree.BuilderNode node, bool passthrough)
        {
            node.Passthrough &= passthrough;
            
            if (node.Slot.IsSome)
            {
                return node.Slot.AsT0;
            }

            var slot = NumberOfSlots++;
            node.Slot = slot;
            return slot;
        }

        private Expression BuildBinary(Expression.Binary binary) => 
            new Expression.Binary(binary.Operator, Build(binary.Left, false), Build(binary.Right, false));

        private Expression BuildPresence(Expression.Presence presence)
        {
            var expr = Build(presence.Expression, false);

            return new Expression.Presence(expr, presence.Negate);
        }
        
        private Expression BuildIn(Expression.In @in)
        {
            var expr = Build(@in.Expression, false);
            var matches = @in.Matches.Select(x => Build(x, false)).ToArray();
            
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