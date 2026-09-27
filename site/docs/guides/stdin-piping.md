---
title: Stdin Piping
layout: simple
---

# Stdin Piping

CliInvoke feeds standard input to a child process by copying the
`StreamWriter` you put in `ProcessConfiguration.StandardInput` into the
child's stdin pipe. When the copy ends, CliInvoke closes the child's
stdin so the child receives end-of-file and stops waiting for input.

This guide covers which paths pipe stdin, how to set one up, and what
you own afterwards.

If you only read one section, read
[Which paths pipe stdin](#which-paths-pipe-stdin).

## Prerequisites

Piping needs both of these on the configuration:

- `StandardInput`, the `StreamWriter` whose content the child reads.
- `RedirectStandardInput = true`, the switch that enables redirection.
  With redirection left off, a configured `StandardInput` is a
  documented no-op.

## Which paths pipe stdin

Every run-to-completion path copies the configured `StandardInput`. The
start-only members do not.

| Path | Pipes stdin? | When the copy runs |
|------|--------------|--------------------|
| Raw: `CliRun.RunAsync`, `IProcessInvoker.ExecuteAsync` | Yes | During `StartAsync(CancellationToken)`, before the run waits for exit. |
| Buffered: `CliRun.RunBufferedAsync`, `IProcessInvoker.ExecuteBufferedAsync` | Yes | During `CaptureBufferedResultAsync`, concurrently with output capture. |
| `IExternalProcess.StartAsync(CancellationToken)` | Yes | After launch, before the call returns. |
| `IExternalProcess.StartAsync(ProcessConfiguration, CancellationToken)` | Yes | After launch, before the call returns. |
| `IExternalProcess.CaptureBufferedResultAsync(...)` | Yes | Alongside the output drain and the wait for exit. |
| `IExternalProcess.Start()` | No | Never; start-only by design (see below). |
| `CliRun.FireAndForget` | No | Never; it returns the process id immediately. |

`StartAsync` returns at launch, not at exit. It completes once the
process is running and the stdin copy has been delivered, and it never
produces a process result. Obtain the result separately with
`WaitForExitOrTimeoutAsync` or `CaptureBufferedResultAsync`.

## End-of-file on every exit path

Whenever the copy stops, whether the source ran out of data, the
invocation was cancelled, or reading the source threw, CliInvoke closes
the child's stdin write end in a `finally`. That close delivers
end-of-file. A child that reads stdin until EOF finishes instead of
waiting forever. The close targets the child's pipe only, so it is safe
even when the child has already exited.

Cancelling an invocation still ends the child's input. A read error in
your source stream propagates as an exception from the call. A broken
pipe that the library's own timeout or cancellation kill caused is
absorbed at the join instead.

## Example: Raw

```csharp
using System.Text;
using CliInvoke;
using CliInvoke.Core;

byte[] payload = Encoding.UTF8.GetBytes("alpha\nbeta\ngamma\n");
using MemoryStream source = new(payload);
using StreamWriter stdin = new(source, Encoding.UTF8, leaveOpen: true);

ProcessConfiguration configuration = new("cat")
{
    RedirectStandardInput = true,
    StandardInput = stdin
};

ProcessResult result = await CliRun.RunAsync(configuration);
Console.WriteLine($"exit code: {result.ExitCode}");
```

The copy reads the writer's underlying stream from its current
position, so the stream must already be positioned at the payload. A
pre-filled `MemoryStream` like the one above is. If you wrote through
the `StreamWriter` yourself, call `Flush()` and rewind
(`source.Position = 0`) before invoking.

`cat` is Unix. On Windows, `findstr .` reads its input from stdin the
same way.

## Example: Buffered

```csharp
using System.Text;
using CliInvoke;
using CliInvoke.Core;

byte[] payload = Encoding.UTF8.GetBytes("alpha\nbeta\ngamma\n");
using MemoryStream source = new(payload);
using StreamWriter stdin = new(source, Encoding.UTF8, leaveOpen: true);

ProcessConfiguration configuration = new("cat")
{
    RedirectStandardInput = true,
    StandardInput = stdin
};

BufferedProcessResult result = await CliRun.RunBufferedAsync(configuration);

Console.Write(result.StandardOutput);   // echoes the three lines
Console.Error.Write(result.StandardError);
```

The child reads stdin while CliInvoke reads its stdout, so a child that
alternates between writing output and consuming input keeps making
progress on both.

## Why `Start()` does not pipe

`IExternalProcess.Start()` and `CliRun.FireAndForget` start the process
and return without touching stdin. That is deliberate:

- A synchronous copy inside `Start()` would block the caller until the
  child consumed the entire payload, at the child's reading speed and
  with no timeout machinery inside `Start()` to bound it.
- The Buffered composition calls `Start()` before anything drains the
  child's output. If `Start()` copied stdin first, a child that fills
  its stdout pipe before consuming all of its input would stall the
  copy, and the drain that could unblock it only begins afterwards. The
  composition would deadlock on exactly the input/output shape piping is
  meant to serve.
- `FireAndForget` returns the process id immediately by contract. A
  blocking copy would make it wait for the child to catch up.

The copy therefore runs in the members that already coordinate
concurrent work: `StartAsync` and `CaptureBufferedResultAsync`.

## Why Buffered copies concurrently

`CaptureBufferedResultAsync` starts three tasks together: the stdin
copy, the stdout/stderr drain, and the wait for exit. It joins all three
before it returns. Copying stdin alongside the drain means neither side
runs ahead of the child. The child reads input and writes output at its
own pace, and CliInvoke feeds and drains at that same pace. Copying
stdin before the drain begins, or waiting for exit before copying, would
let one side block the other, which is the deadlock the composition
avoids.

## Who owns the source stream

The `StreamWriter` you assign to `StandardInput` stays yours:

- CliInvoke reads bytes from it and never closes or disposes it. The
  end-of-file close targets the child's pipe only, not your stream.
- Dispose it yourself once the final invocation that referenced it has
  completed, ideally in its own `using` declaration.
- Reusing one configuration, and its `StandardInput`, across several
  invocations is fine. Do not dispose the stream between them.

This matches the [Resource Disposal](./resource-disposal.md) guide. The
[Configuration](./configuration.md) guide documents the same ownership
rule on the `ProcessConfiguration` reference and the `IExternalProcess`
lifecycle steps.
