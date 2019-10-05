using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Newtonsoft.Json;
using SelectQuery.Distribution;
using SelectQuery.Lambda.Inputs;
using SelectQuery.Lambda.Outputs;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Implementations
{
    internal class LambdaWorkerExecutor : IWorkerExecutor
    {
        private readonly IAmazonLambda _lambda;
        private readonly string _workerFunctionName;

        public LambdaWorkerExecutor(IAmazonLambda lambda, string workerFunctionName)
        {
            _lambda = lambda;
            _workerFunctionName = workerFunctionName;
        }

        public async Task<IReadOnlyCollection<Result>> ExecuteAsync(DistributorPlan plan, IReadOnlyList<Uri> sources)
        {
            var tasks = new Task<Result>[sources.Count];

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];

                tasks[i] = ExecuteAsync(plan, source);
            }

            return await Task.WhenAll(tasks);
        }

        private async Task<Result> ExecuteAsync(DistributorPlan plan, Uri source)
        {
            var serializer = new JsonSerializer();

            var input = new WorkerPublicInput
            {
                UnderlyingQuery = plan.UnderlyingQuery.ToString(),
                Order = plan.Order.IsSome ? plan.Order.AsT0.ToString() : null,
                Limit = plan.Limit.IsSome ? plan.Limit.AsT0.ToString() : null,

                DataLocation = source
            };

            var response = await _lambda.InvokeAsync(new InvokeRequest
            {
                FunctionName = _workerFunctionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonConvert.SerializeObject(input),
            });

            if (!string.IsNullOrEmpty(response.FunctionError))
            {
                throw new WorkerException(response.FunctionError);
            }

            using var streamReader = new StreamReader(response.Payload);
            using var reader = new JsonTextReader(streamReader);

            var result = serializer.Deserialize<PublicResult>(reader);

            return result.Location == null 
                ? (Result)new Result.Direct(result.Rows?.Select(x => new ResultRow(x)).ToList() ?? new List<ResultRow>()) 
                : new Result.InDirect(result.Location);
        }
    }

    internal class WorkerException : Exception
    {
        public WorkerException(string message) : base(message) { }
    }
}