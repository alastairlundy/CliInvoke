---
title: Results
layout: simple
---

# Working with Results

When a process exits, CliInvoke returns an immutable result object. Two
concrete types exist — `ProcessResult` and
`BufferedProcessResult` — corresponding to the two execution modes
(plain and buffered). This page documents the result types, their
convenience members, and the recommended way to test for success.

If you only read one section, read
[Exit-code convenience vs. caller-stated validation](#exit-code-convenience-vs-caller-stated-validation).

## Result types at a glance

| Type | Returns stdout/stderr? | Use when |
|------|------------------------|----------|
| `ProcessResult` | No | You only need the exit code, process id, and timing. |
| `BufferedProcessResult` | Yes (`StandardOutput`, `StandardError`) | You need the captured text output. |

`BufferedProcessResult` inherits from `ProcessResult`, so every member
on `ProcessResult` is also available on a buffered result.

## Exit-code convenience vs. caller-stated validation

CliInvoke provides two distinct ways to test whether a process
succeeded. They serve different audiences and should not be confused.

### Default heuristic — `IsExitCodeZero` / `EnsureExitCodeZero`

Extension methods on every `ProcessResult` (and therefore on
`BufferedProcessResult` too):

```csharp
using CliInvoke;
using CliInvoke.Core;

BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--version");

// Boolean check — returns true when ExitCode == 0.
if (result.IsExitCodeZero())
{
    Console.WriteLine(result.StandardOutput);
}

// Guard — throws ProcessNotSuccessfulException<BufferedProcessResult>
// when ExitCode != 0.
result.EnsureExitCodeZero();
```

`IsExitCodeZero` is a literal exit-code check with no `Canceled`
clause. `EnsureExitCodeZero` throws
`ProcessNotSuccessfulException<TProcessResult>` when the exit code is
non-zero; the exception carries the full `ProcessExceptionInfo` (result
and optional configuration) for diagnostics.

These methods are the recommended quick-check for scripts, CI steps, and
non-DI call sites. They are a **default heuristic**, not a success
policy: they test one boolean condition and know nothing about what the
caller considers successful.

### Authoritative validation — `ThrowIfUnsuccessful` / `UsePostExitValidation`

When success depends on more than the exit code — for example, a
specific exit code, a stdout regex, or an empty stderr — use the
caller-stated validation path instead.

**Inline (non-DI):**

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Validation;

BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--list-sdks");

IProcessResultValidator<BufferedProcessResult> validator =
    new ProcessResultValidator<BufferedProcessResult>(
        [CommonValidationRules<BufferedProcessResult>.ExitCodeZeroRule()]);

// Throws ProcessNotSuccessfulException if validation fails.
result.ThrowIfUnsuccessful(validator);
```

**Middleware (DI):**

```csharp
using CliInvoke;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware.Validation;

services.AddCliInvoke(builder =>
{
    builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
});
```

`UsePostExitValidation` runs after the process exits and throws
`ProcessValidationException` (with per-rule failure messages) when the
validator rejects the result. Helpers: `PostExitValidation.ExitCodeIsZero()`,
`ExitCodeIs(code)`, `ExitCodeIsOneOf(codes...)`, `StdoutMatches(regex)`,
`StderrIsEmpty()`.

> **Which should I pick?** Use `IsExitCodeZero` / `EnsureExitCodeZero`
> for quick, script-style checks where "exit code 0 means success" is
> correct. Use `ThrowIfUnsuccessful` or `UsePostExitValidation` when
> the success criterion is a policy the caller states explicitly — for
> example, "only exit code 0 and 1 are acceptable" or "stderr must be
> empty". The convenience methods never replace a caller-stated
> validator; they coexist.

## `ToString` — compact diagnostic format

Both result types override `ToString` to produce a compact, single-line
diagnostic string. The format is fixed (intentionally suitable for
logging and test assertions) and never embeds output content.

### `ProcessResult.ToString()`

```
[ExitCode=0, Path=/usr/bin/dotnet, Runtime=00:00:01.234]
```

Bracketed fields: exit code, executed file path, and wall-clock runtime
duration.

### `BufferedProcessResult.ToString()`

```
[ExitCode=0, Path=/usr/bin/dotnet, Runtime=00:00:01.234, StdOutLen=42, StdErrLen=0]
```

Inherits the base format and appends stdout/stderr character counts. When
the output was truncated by a per-stream cap (see output-truncation
middleware), a `Truncated=true` indicator appears:

```
[ExitCode=0, Path=/usr/bin/dotnet, Runtime=00:00:01.234, StdOutLen=1048576, StdErrLen=0, Truncated=true]
```

The lengths are character counts, not byte counts. The output text
itself is never included — safe for structured logging pipelines.

## `Deconstruct` — pattern matching on `BufferedProcessResult`

`BufferedProcessResult` provides a `Deconstruct` that yields the three
fields most callers care about:

```csharp
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--version");

// Deconstruct into exit code, stdout, and stderr.
var (exitCode, stdout, stderr) = result;

if (exitCode == 0)
{
    Console.WriteLine(stdout);
}
```

The deconstructed tuple is `(int exitCode, string stdout, string
stderr)`.

## Lazy line enumeration — `EnumerateOutputLines` / `EnumerateErrorLines`

`BufferedProcessResult` exposes lazy line-enumeration extensions that
split on `Environment.NewLine` without allocating an intermediate array:

```csharp
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet", "--list-sdks");

foreach (string line in result.EnumerateOutputLines())
{
    Console.WriteLine($"SDK: {line}");
}

foreach (string line in result.EnumerateErrorLines())
{
    Console.Error.WriteLine($"WARN: {line}");
}
```

These produce the same results as `string.Split(Environment.NewLine)`,
including a trailing empty element when the input ends with a newline.
They are deliberately **not** `MemoryExtensions.EnumerateLines` — that
API treats lone `\n` as a separator, which diverges from the
`GetOutputLines()` contract. The lazy variants are the right choice when
you want to stream-process output line-by-line without materialising the
full array.

For the eager counterpart that returns both streams at once, see the
existing `GetOutputLines()` method on `BufferedProcessResult`:

```csharp
(string[] stdoutLines, string[] stderrLines) = result.GetOutputLines();
```

## Other `BufferedProcessResult` helpers

| Method | Returns | Purpose |
|--------|---------|---------|
| `GetFirstOutputLine()` | `string` | First line of stdout (no allocation for the full array). |
| `GetOutputLines()` | `(string[], string[])` | Eager split of stdout and stderr into line arrays. |
| `HasErrors()` | `bool` | `true` when `StandardError` is non-empty. |

## Cross-references

- [Choosing your Invocation Pattern](choosing-invocation-pattern.md) —
  which pattern to use and when.
- [Configuration](configuration.md) — the configuration models that
  produce results.
- [Middleware](middleware.md) — `UsePostExitValidation` and
  `UseLogging` for result-aware middleware.
- [Architecture](architecture.md) — the data-flow from configuration
  to result.
- Source files:
  - `src/CliInvoke.Core/Primitives/Results/ProcessResult.cs`
  - `src/CliInvoke.Core/Primitives/Results/BufferedProcessResult.cs`
  - `src/CliInvoke.Core/Extensions/ProcessResultHelperExtensions.cs`
