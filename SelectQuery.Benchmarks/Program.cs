using BenchmarkDotNet.Running;
using SelectQuery.Benchmarks;

var summary = BenchmarkRunner.Run<EvaluationBenchmarks>();