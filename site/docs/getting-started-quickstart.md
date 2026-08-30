---
title: Getting Started - Quickstart
layout: simple
---

## Quickstart

Install the package via NuGet:

```xml
<PackageReference Include="CliInvoke" Version="3.0.0" />
```

Add services:

```csharp
var services = new ServiceCollection();
services.AddCliInvoke();
var provider = services.BuildServiceProvider();
```

The quickest way to run a command is `CliRun` — the recommended default entry point:

```csharp
using CliInvoke;

// Run a command and wait for it to finish
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
Console.WriteLine(result.ExitCode);

// Or capture stdout/stderr
BufferedProcessResult output = await CliRun.RunBufferedAsync("dotnet", "--info");
Console.WriteLine(output.StandardOutput);
```

`CliRun` needs no dependency injection and no factories. When you need DI, middleware, or
process-level control, see the full Getting Started guide and [PATTERNS.md](PATTERNS.md).

(Quickstart migrated from existing docs.)
