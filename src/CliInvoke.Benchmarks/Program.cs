// See https://aka.ms/new-console-template for more information

using System.Reflection;
using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions;

using BenchmarkDotNet.Running;

using CliInvoke.Benchmarking.Benchmarks.Invokation;


BenchmarkRunner.Run<DotnetUnbufferedInvokationBenchmark>();
//BenchmarkRunner.Run(Assembly.GetExecutingAssembly());