# SelectQuery

![nuget](https://img.shields.io/nuget/v/SelectQuery.Evaluation) ![licence](https://img.shields.io/github/license/will14smith/SelectQuery)

A .NET library for mocking S3 Select queries

The library is not 100% feature complete with S3 Select but it supports a decent set of operations, check out the [evaluator tests](https://github.com/will14smith/SelectQuery/blob/master/SelectQuery.Evaluation.Tests/EvaluatorTests.cs) for the full details:
- Json lines object inputs (not gzipped)
- Json output
- Column projection (e.g. `SELECT s.Id, s.Name.First as N ...`)
- Basic filtering (e.g. `WHERE s.Id > 0 AND s.Name.First IS NOT NULL`)
- Like filtering (e.g. `WHERE s.Name.First LIKE 'J%'`)
- Aggregation Functions (e.g. `SELECT AVG(s.Doors) ...`)
- Scalar Functions (e.g. `SELECT LOWER(s.Name.First) ...`)
- Limits (e.g. `LIMIT 10`)

## Usage

Install the [`SelectQuery.Evaluation`](https://www.nuget.org/packages/SelectQuery.Evaluation/) package from nuget

## Examples

```csharp
var queryResult = Parser.Parse("SELECT s.Id, s.Name FROM s3object s WHERE s.Doors > 3");
// in production code you will likely want to check the result for errors
var query = queryResult.Value;

var evaluator = new JsonLinesEvaluator(query);
var output = evaluator.Run(input);
```

The `input` must be a UTF-8 encoded json lines formatter byte array, and the `output` is also in that format

An example interface to embed this in an application could be as follows, with one implementation using the actual S3 client and another one being against an in-memory dictionary and this library
```csharp
interface IS3
{
    Task PutAsync(string key, byte[] file);
    Task<string[]> SelectJsonLinesAsync(string key, string query);
}

class TestS3 : IS3
{
    private readonly Dictionary<string, byte[]> _objects = new();

    public async Task PutAsync(string key, byte[] file) => _objects[key] = file;
    public async Task<string[]> SelectJsonLinesAsync(string key, string queryStr)
    {
        var query = Parser.Parse(queryStr).Value;
        var evaluator = new JsonLinesEvaluator(query);
        var output = evaluator.Run(_objects[key]);
    
        var outputStr = Encoding.UTF8.GetString(output);
        return outputStr.Split('\n');
    }
}
```