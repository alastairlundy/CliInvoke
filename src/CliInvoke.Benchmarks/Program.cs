// See https://aka.ms/new-console-template for more information

using System.Reflection;
using BenchmarkDotNet.Running;

using CliInvoke.Benchmarking.Benchmarks.Invokation;


BenchmarkRunner.Run<BasicUnbufferedInvokationBenchmark>();
//BenchmarkRunner.Run(Assembly.GetExecutingAssembly());