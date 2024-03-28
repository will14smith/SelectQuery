namespace SelectQuery.Evaluation.Trees;

public abstract class QueryTreeExpression
{
    public class IndexedOutput : QueryTreeExpression
    {
        public int Index { get; }
        public IndexedOutput(int index) => Index = index;
    }
}