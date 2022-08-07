using System.Linq;
using System.Text;
using SelectParser;
using SelectParser.Queries;
using Xunit;

namespace SelectQuery.Evaluation.Tests;

public class EvaluatorTests
{
    const string ProjectionRecord = @"{""a"":1,""b"":2,""c"":[1,2,3],""d"":{""d1"":""d1"",""d2"":{""d2.2"":true},""d3"":3}}";

    [Theory]

    [InlineData("SELECT 1 FROM s3object", @"{""_1"":1}")]
    [InlineData("SELECT 1, 'a', true FROM s3object", @"{""_1"":1,""_2"":""a"",""_3"":true}")]
    [InlineData("SELECT 1 as a, 'a' FROM s3object", @"{""a"":1,""_2"":""a""}")]
    public void StaticDataQueries(string queryString, string expectedRecord)
    {
        var query = ParseQuery(queryString);
        var data = new[] {@"{}", @"{}", @"{}"};
        var expectedResponse = new[] {expectedRecord, expectedRecord, expectedRecord};

        var response = Evaluate(query, data);

        AssertResponse(expectedResponse, response);
    }

    [Theory]

    [InlineData("SELECT * FROM s3object", ProjectionRecord)]
    [InlineData("SELECT s.a FROM s3object s", @"{""a"":1}")]
    [InlineData("SELECT s.A FROM s3object s", @"{""A"":1}")]
    [InlineData("SELECT s.x.a FROM s3object s", @"{}")]
    [InlineData("SELECT s.a as b FROM s3object s", @"{""b"":1}")]
    [InlineData("SELECT s.a, s.c FROM s3object s", @"{""a"":1,""c"":[1,2,3]}")]
    [InlineData("SELECT s.a as b, s.b as a FROM s3object s", @"{""b"":1,""a"":2}")]
    [InlineData(@"SELECT s.d.d1, s.d.d2.""d2.2"" FROM s3object s", @"{""d1"":""d1"",""d2.2"":true}")]
    public void ProjectionQueries(string queryString, string expectedRecord)
    {
        var query = ParseQuery(queryString);
        var data = new[] {ProjectionRecord};
        var expectedResponse = new[] {expectedRecord};

        var response = Evaluate(query, data);

        AssertResponse(expectedResponse, response);
    }

    [Theory]

    [InlineData("SELECT 1 FROM s3object WHERE s3object.a = 1", new [] { @"{""a"":1}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 1", new [] { @"{""a"":1}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 2", new [] { @"{""a"":1}" }, 0)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 1", new [] { @"{""a"":1}", @"{""a"":2}", @"{""a"":3}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 1 and s.a < 3", new [] { @"{""a"":1}", @"{""a"":2}", @"{""a"":3}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 1 or s.a >= 3", new [] { @"{""a"":1}", @"{""a"":2}", @"{""a"":3}" }, 2)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a in (1, 2)", new [] { @"{""a"":1}", @"{""a"":2}", @"{""a"":3}" }, 2)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a IS NULL", new [] { @"{""a"":1}", @"{""b"":2}", @"{""a"":null}" }, 2)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a IS NOT NULL", new [] { @"{""a"":1}", @"{""b"":2}", @"{""a"":null}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a IS MISSING", new [] { @"{""a"":1}", @"{""b"":2}", @"{""a"":3}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a IS NOT MISSING", new [] { @"{""a"":1}", @"{""b"":2}", @"{""a"":3}" }, 2)]
    public void WhereQueries(string queryString, string[] records, int expectedCount)
    {
        var query = ParseQuery(queryString);

        var response = Evaluate(query, records);

        Assert.Equal(expectedCount, response.Count(x => x == '\n'));
    }   
        
    [Theory]

    [InlineData("SELECT 1 FROM s3object s LIMIT 2", new [] { @"{""a"":1}" }, 1)]
    [InlineData("SELECT 1 FROM s3object s LIMIT 2", new [] { @"{""a"":1}", @"{""a"":1}", @"{""a"":1}", @"{""a"":1}" }, 2)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a = 1 LIMIT 2", new [] { @"{""a"":2}", @"{""a"":1}", @"{""a"":1}" }, 2)]
    public void LimitQueries(string queryString, string[] records, int expectedCount)
    {
        var query = ParseQuery(queryString);

        var response = Evaluate(query, records);

        Assert.Equal(expectedCount, response.Count(x => x == '\n'));
    }

    [Theory]

    [InlineData("SELECT AVG(s.a) FROM s3object s", true, @"{""_1"":10}")]
    [InlineData("SELECT AVG(s.a) FROM s3object s", false, @"{""_1"":null}")]
    [InlineData("SELECT COUNT(*) FROM s3object s", true, @"{""_1"":5}")]
    [InlineData("SELECT COUNT(*) FROM s3object s LIMIT 1", true, @"{""_1"":1}")]
    [InlineData("SELECT COUNT(*) FROM s3object s", false, @"{""_1"":0}")]
    [InlineData("SELECT COUNT(s.a) FROM s3object s", true, @"{""_1"":3}")]
    [InlineData("SELECT COUNT(s.a) FROM s3object s", false, @"{""_1"":0}")]
    [InlineData("SELECT MAX(s.a) FROM s3object s", true, @"{""_1"":15}")]
    [InlineData("SELECT MAX(s.a) FROM s3object s", false, @"{""_1"":null}")]
    [InlineData("SELECT MIN(s.a) FROM s3object s", true, @"{""_1"":5}")]
    [InlineData("SELECT MIN(s.a) FROM s3object s", false, @"{""_1"":null}")]
    [InlineData("SELECT SUM(s.a) FROM s3object s", true, @"{""_1"":30}")]
    [InlineData("SELECT SUM(s.a) FROM s3object s", false, @"{""_1"":null}")]
    [InlineData("SELECT MAX(s.a), COUNT(*) as NumOfItems, SUM(s.a) FROM s3object s", true, @"{""_1"":15,""NumOfItems"":5,""_3"":30}")]
    [InlineData("SELECT MAX(s.a), COUNT(*) as NumOfItems, SUM(s.a) FROM s3object s", false, @"{""_1"":null,""NumOfItems"":0,""_3"":null}")]
    public void AggregateQueries(string queryString, bool withData, string expectedRecord)
    {
        var query = ParseQuery(queryString);
        var data = withData ? new [] { @"{""a"": 5}", @"{""a"": 10}", @"{""a"": 15}", @"{}", @"{""b"": 5}" } : new string[] { };
        var expectedResponse = new[] {expectedRecord};

        var response = Evaluate(query, data);

        AssertResponse(expectedResponse, response);
    }
        
    [Theory]

    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE 'a'", 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE '_a'", 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE 'a_'", 1)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE '%a'", 3)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE 'a%'", 3)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE '%a%'", 5)]
    [InlineData("SELECT 1 FROM s3object s WHERE s.a LIKE '%x^%' ESCAPE '^'", 2)]
    public void LikeQueries(string queryString, int expectedCount)
    {
        var records = new[]
        {
            "", "a", "ba", "ab", "abb", "bba", "x", "x%", "cx", "cx%"
        }.Select(x => $@"{{""a"":""{x}""}}").ToArray();
            
        var query = ParseQuery(queryString);

        var response = Evaluate(query, records);

        Assert.Equal(expectedCount, response.Count(x => x == '\n'));
    }
        
    private static Query ParseQuery(string query)
    {
        var tokens = new SelectTokenizer().Tokenize(query);
        return Parser.Query(tokens).Value;
    }

    private static byte[] Evaluate(Query query, string[] data)
    {
        var evaluator = new JsonLinesEvaluator(query);
        return evaluator.Run(Encoding.UTF8.GetBytes(RecordsToString(data)));
    }

    private void AssertResponse(string[] expected, byte[] actual)
    {
        Assert.Equal(RecordsToString(expected), Encoding.UTF8.GetString(actual));
    }

    private static string RecordsToString(string[] expected)
    {
        return string.Join("\n", expected) + "\n";
    }
}