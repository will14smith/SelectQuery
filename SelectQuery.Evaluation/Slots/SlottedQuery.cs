using System.Collections.Generic;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQuery
    {
        public SlottedQuery(SelectClause slottedSelect, Option<Expression> slottedPredicate, IReadOnlyList<Expression> slotExpressions)
        {
            SlottedSelect = slottedSelect;
            SlottedPredicate = slottedPredicate;
            SlotExpressions = slotExpressions;
        }

        public SelectClause SlottedSelect { get; }
        public Option<Expression> SlottedPredicate { get; }
        
        public IReadOnlyList<Expression> SlotExpressions { get; }
    }
}