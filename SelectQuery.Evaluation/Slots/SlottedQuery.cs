using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedQuery
    {
        public SlottedQuery(SelectClause slottedSelect, Option<Expression> slottedPredicate, SlottedExpressionTree slotTree, int numberOfSlots)
        {
            SlottedSelect = slottedSelect;
            SlottedPredicate = slottedPredicate;
            SlotTree = slotTree;
            NumberOfSlots = numberOfSlots;
        }

        public SelectClause SlottedSelect { get; }
        public Option<Expression> SlottedPredicate { get; }
        public SlottedExpressionTree SlotTree { get; }
        public int NumberOfSlots { get; }
    }
}