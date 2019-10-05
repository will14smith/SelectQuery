using System.Linq;
using System.Threading.Tasks;
using OneOf.Types;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Lambda.Implementations;
using SelectQuery.Lambda.Inputs;
using SelectQuery.Lambda.Outputs;
using SelectQuery.Results;
using SelectQuery.Workers;
using Superpower;

namespace SelectQuery.Lambda
{
    public class WorkerEntryPoint
    {
        private readonly Worker _worker;

        public WorkerEntryPoint()
        {
            var executor = CreateExecutor();
            var storage = CreateStorage();

            _worker = new Worker(executor, storage);
        }

        public WorkerEntryPoint(IUnderlyingExecutor executor, IResultsStorer resultsStorer)
        {
            _worker = new Worker(executor, resultsStorer);
        }

        private static S3SelectExecutor CreateExecutor()
        {
            return new S3SelectExecutor();
        }
        private static S3ResultStorage CreateStorage()
        {
            return new S3ResultStorage();
        }

        public async Task<PublicResult> Handler(WorkerPublicInput input)
        {
            var queryInput = ConvertInput(input);

            var result = await _worker.QueryAsync(queryInput);

            return ConvertOutput(result);
        }

        private static WorkerInput ConvertInput(WorkerPublicInput input)
        {
            var underlyingQuery = Parse(Parser.Query, input.UnderlyingQuery);
            var order = string.IsNullOrEmpty(input.Order)
                ? (Option<OrderClause>)new None()
                : Parse(Parser.OrderByClause, input.Order);
            var limit = string.IsNullOrEmpty(input.Limit)
                ? (Option<LimitClause>)new None()
                : Parse(Parser.LimitClause, input.Limit);

            var plan = new WorkerPlan(underlyingQuery, order, limit);

            return new WorkerInput(plan, input.DataLocation);
        }

        private static PublicResult ConvertOutput(Result result)
        {
            return result.Match(
                direct => new PublicResult { Rows = direct.Rows.Select(x => x.Fields).ToList() },
                indirect => new PublicResult { Location = indirect.Location }
            );
        }

        private static T Parse<T>(TokenListParser<SelectToken, T> parser, string input)
        {
            var tokenizer = new SelectTokenizer();
            var tokens = tokenizer.Tokenize(input);
            return parser.Parse(tokens);
        }
    }
}
