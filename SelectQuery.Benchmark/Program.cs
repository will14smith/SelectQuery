using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Evaluation;

namespace SelectQuery.Benchmark
{
    [MemoryDiagnoser]
    public class OldVsNew
    {
        [Params(@"SELECT s.d.d1, s.""d"".d2.""d2.2"" FROM s3object s", @"SELECT * FROM s3object s WHERE s.a = 1 and s.a < 3")]
        public string Query;

        [Params(1, 2000)] 
        public int RecordSize;
        
        private byte[] _data;
        private Query _query;

        private JsonLinesEvaluator _old; 
        private JsonLinesEvaluatorNew _new;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _query = Parser.Query(new SelectTokenizer().Tokenize(Query)).Value;

            _old = new JsonLinesEvaluator(_query);
            _new = new JsonLinesEvaluatorNew(_query);
            
            var data = @"{""a"":1,""b"":2,""c"":[1,2,3],""d"":{""d1"":""d1"",""d2"":{""d2.2"":true},""d3"":3}," + string.Join(",", Enumerable.Range(1, RecordSize).Select(x => $"\"n{x}\": {(x % 2 == 0 ? $"{x*397}" : $"\"xxx{x}xxx\"")}")) + "}";
            _data = Encoding.UTF8.GetBytes(string.Join("\n", Enumerable.Repeat(data, 100)) + "\n");
        }

        [Benchmark]
        public byte[] Old() => _old.Run(_data);
        [Benchmark]
        public byte[] New() => _new.Run(_data);
        
        [Benchmark]
        public byte[] OldNoCache() => new JsonLinesEvaluator(_query).Run(_data);
        [Benchmark]
        public byte[] NewNoCache() => new JsonLinesEvaluatorNew(_query).Run(_data);

    }
    
    class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<OldVsNew>();
        }
    }
}