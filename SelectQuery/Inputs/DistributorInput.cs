using SelectParser.Queries;

namespace SelectQuery.Inputs
{
    public class DistributorInput
    {
        public DistributorInput(Query query, DataSource source)
        {
            Query = query;
            Source = source;
        }

        public Query Query { get; }
        public DataSource Source { get; }
    }
}