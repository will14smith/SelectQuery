using System.Collections.Generic;
using System.Linq;
using System.Text;
using SelectParser;
using Xunit;

namespace SelectQuery.Evaluation.Tests;

public class ProfilerTests
{
    [Fact]
    public void T()
    {
        var queryString = $"SELECT * FROM s3Object s WHERE s.c0 IN ({string.Join(", ", Enumerable.Range(0, 2000).Select(x => $"'str{x}'"))})";
        
        var largeRecords = new List<string>();
        for (var i = 0; i < 5_000; i++)
        {
            largeRecords.Add($"{{\"a\":1,\"b\":2,\"c\":3,{string.Join(",", Enumerable.Range(0, 300).Select(x => $"\"c{x}\": \"str{((x^i)*7)}\""))}}}");
        }
        var input = Encoding.UTF8.GetBytes(RecordsToString(largeRecords));
        
        var query = Parser.Parse(queryString).Value;
        
        var evaluator = new JsonLinesEvaluator(query);
        var output = evaluator.Run(input);
        
        Assert.True(output.Length > 0);
    }

    private static string RecordsToString(IEnumerable<string> expected) => string.Join("\n", expected) + "\n";
}