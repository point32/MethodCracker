// You must run this project under 'Release' configuration, or 'BenchmarkDotNet' will raise an exception.

using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using MethodCracker;
using MethodCrackerBenchmark;

new Test().BenchmarkFooWithHooks();
Summary sum = BenchmarkRunner.Run<Test>();
Console.Clear();
Console.WriteLine(sum);