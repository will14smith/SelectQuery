using System.Text;
using BenchmarkDotNet.Attributes;
using SelectParser;
using SelectParser.Queries;
using SelectQuery.Evaluation;

namespace SelectQuery.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class EvaluationBenchmarks
{
    [ParamsSource(nameof(ValuesForTestCasesInput))]
    public TestCaseInput Input { get; set; }
    [ParamsSource(nameof(ValuesForTestCases))]
    public TestCase Query { get; set; }

    public static IEnumerable<TestCaseInput> ValuesForTestCasesInput
    {
        get
        {
            var smallRecords = Enumerable.Repeat("{\"a\":1,\"b\":2,\"c\":3,\"c0\":\"str0\",\"d\":{\"d1\":\"d1\",\"d2\":{\"d2.2\":true},\"d3\":[1,2,3]}}", 100);

            var largeRecords = new List<string>();
            for (var i = 0; i < 5_000; i++)
            {
                largeRecords.Add($"{{\"a\":1,\"b\":2,\"c\":3,{string.Join(",", Enumerable.Range(0, 300).Select(x => $"\"c{x}\": \"str{(x^i)*7}\""))}}}");
            }
            
            yield return new TestCaseInput("small", Encoding.UTF8.GetBytes(RecordsToString(smallRecords)));
            yield return new TestCaseInput("large", Encoding.UTF8.GetBytes(RecordsToString(largeRecords)));
        }
    }
    
    public static IEnumerable<TestCase> ValuesForTestCases
    {
        get
        {
            yield return new TestCase("simple", "SELECT * FROM s3Object");
            yield return new TestCase("projection", "SELECT s.a, s.b, s.c FROM s3Object s");
            yield return new TestCase("filtered", "SELECT * FROM s3Object s WHERE s.a = 1 AND s.b = true AND s.c = 'def'");
            yield return new TestCase("aggregated", "SELECT MAX(s.a), SUM(s.b), AVG(s.c) FROM s3Object s");
            
            yield return new TestCase("large projection", $"SELECT {string.Join(", ", Enumerable.Range(0, 300).Select(x => $"s.c{x}"))} FROM s3Object s");
            yield return new TestCase("large IN filter", $"SELECT * FROM s3Object s WHERE s.c0 IN ({string.Join(", ", Enumerable.Range(0, 2000).Select(x => $"'str{x}'"))})");
        }
    }

    
    [Benchmark]
    public byte[] Run() => new JsonLinesEvaluator(Query.Query).Run(Input.Input);

    public static string RecordsToString(IEnumerable<string> expected) => string.Join("\n", expected) + "\n";

    public class TestCase(string name, string queryString)
    {
        public string Name { get; } = name;
        public Query Query { get; } = Parser.Parse(queryString).Value!;

        public override string ToString() => Name;
    }   
    public class TestCaseInput(string name, byte[] input)
    {
        public string Name { get; } = name;
        public byte[] Input { get; } = input;

        public override string ToString() => Name;
    }
}