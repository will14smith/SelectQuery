﻿using BenchmarkDotNet.Attributes;
using SelectParser;
using SelectParser.Queries;

namespace SelectQuery.Benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class ParsingBenchmarks
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
    
    public class TestCase(string name, string queryString)
    {
        public string Name { get; } = name;
        public string QueryString { get; } = queryString;

        public override string ToString() => Name;
    }
}