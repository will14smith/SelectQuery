using BenchmarkDotNet.Attributes;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class QueryParsingBenchmarks
{
    [ParamsSource(nameof(ValuesForTestCases))]
    public TestCase Query { get; set; }

    public static IEnumerable<TestCase> ValuesForTestCases
    {
        get
        {
            yield return new TestCase("simple", "SELECT * FROM s3Object");
            yield return new TestCase("projection", "SELECT s.a, s.b, s.c FROM s3Object s");
            yield return new TestCase("filtered", "SELECT * FROM s3Object s WHERE s.a = 1 AND s.b = true AND s.c = 'def'");
            yield return new TestCase("aggregated", "SELECT MAX(s.a), SUM(s.b), AVG(s.c) FROM s3Object s");
            
            yield return new TestCase("large projection", $"SELECT {string.Join(", ", Enumerable.Range(0, 1000).Select(x => $"s.c{x}"))} FROM s3Object s");
            yield return new TestCase("large IN filter", $"SELECT * FROM s3Object s WHERE s.a IN ({string.Join(", ", Enumerable.Range(0, 1000).Select(x => $"'str{x}'"))})");
        }
    }
    
    [Benchmark]
    public Query Parse() => Parser.Parse(Query.QueryString).Value;

    public class TestCase
    {
        public string Name { get; }
        public string QueryString { get; }

        public TestCase(string name, string queryString)
        {
            Name = name;
            QueryString = queryString;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}