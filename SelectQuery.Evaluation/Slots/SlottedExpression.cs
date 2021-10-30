using SelectParser.Queries;

namespace SelectQuery.Evaluation.Slots
{
    internal class SlottedExpression : Expression
    {
        public int Index { get; }

        public SlottedExpression(int index)
        {
            Index = index;
        }
        
        public override string ToString() => $"$Sloted{Index}";
    }
}