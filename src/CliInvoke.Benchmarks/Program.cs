// See https://aka.ms/new-console-template for more information

using AlastairLundy.CliInvoke.Abstractions;
using AlastairLundy.CliInvoke.Core.Abstractions;

using BenchmarkDotNet.Running;

using CliInvoke.Benchmarking.Benchmarks.Invokation;
using Microsoft.Extensions.DependencyInjection;



BenchmarkRunner.Run<DotnetUnbufferedInvokationBenchmark>();