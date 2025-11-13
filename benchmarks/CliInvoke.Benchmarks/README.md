# CliInvoke Benchmarks

## Notes
All results posted below are run on .NET 9 with the following specs:

All CliInvoke benchmarks show the performance of using ``ProcessFactory`` (merged from Extensions.Processes`` and ``CliCommandInvoker``

```BenchmarkDotNet v0.15.0, Windows 11 (10.0.26100.4061/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 9.0.300
  [Host]     : .NET 8.0.16 (8.0.1625.21506), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.16 (8.0.1625.21506), X64 RyuJIT AVX2```
  

## Basic Unbuffered Invokation Benchmark
This benchmark invokes the ``dotnet`` cli command with ``--list-sdks`` as arguments and,
where supported, throws an exception if the Exit Code is not 0.

