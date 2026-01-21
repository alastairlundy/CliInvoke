using System.Reflection;
using BenchmarkDotNet.Running;
using CliInvoke.Benchmarking.Benchmarks.Invokation;

//BenchmarkRunner.Run<BasicUnbufferedInvokationBenchmark>();
//BenchmarkRunner.Run(Assembly.GetExecutingAssembly());

BenchmarkRunner.Run<BufferedInvokationBenchmark>();