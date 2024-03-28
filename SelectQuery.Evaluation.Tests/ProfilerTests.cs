using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.dotMemoryUnit;
using SelectParser;
using Xunit;
using Xunit.Abstractions;

namespace SelectQuery.Evaluation.Tests;

public class ProfilerTests
{
    private ITestOutputHelper _output;

    public ProfilerTests(ITestOutputHelper output)
    {
        _output = output;
        DotMemoryUnitTestOutput.SetOutputMethod(_output.WriteLine);
    }

    [Fact]
    // [DotMemoryUnit(CollectAllocations = true, SavingStrategy = SavingStrategy.OnAnyFail, Directory = @"C:\dotMemory", FailIfRunWithoutSupport = false)]
    public void T()
    {
        //dotMemory.Check();
        
        // var queryString = $"SELECT * FROM s3Object s WHERE s.c0 IN ({string.Join(", ", Enumerable.Range(0, 2000).Select(x => $"'str{x}'"))})";
        var queryString = $"SELECT {string.Join(", ", Enumerable.Range(0, 300).Select(x => $"s.c{x}"))} FROM s3Object s";
        
        var largeRecords = new List<string>();
        for (var i = 0; i < 5_000; i++)
        {
            largeRecords.Add($"{{\"a\":1,\"b\":2,\"c\":3,{string.Join(",", Enumerable.Range(0, 300).Select(x => $"\"c{x}\": \"str{((x^i)*7)}\""))}}}");
        }
        var input = Encoding.UTF8.GetBytes(RecordsToString(largeRecords));
        
        var query = Parser.Parse(queryString).Value;
        
        //dotMemory.Check();
        
        var evaluator = new JsonLinesEvaluator(query);
        var output = evaluator.Run(input);
        
        Assert.True(output.Length > 0);
        
        //dotMemory.Check();
        
        Assert.False(true);
    }

    private static string RecordsToString(IEnumerable<string> expected) => string.Join("\n", expected) + "\n";
}