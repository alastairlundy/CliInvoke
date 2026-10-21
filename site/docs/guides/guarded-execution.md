---
title: Guarded process execution for automation and agents
layout: simple
---

# Guarded process execution for automation and agents

Code that shells out from an agent harness, a CI runner, an orchestrator, or a
long-running service fails in four predictable ways: children hang forever,
output grows without limit, exit codes go unnoticed, and secrets leak into
logs. `System.Diagnostics.Process` leaves every one of those problems to you.
CliInvoke guards all four by default, and the guards matter most when the
command line is assembled by something that is not a human, such as an agent
that builds shell commands from generated text.

This guide uses the `CliRun` facade because it is the recommended entry point.
Everything here also applies through `IProcessInvoker`, which additionally
gives you the middleware pipeline. See
[Choosing your Invocation Pattern](choosing-invocation-pattern.md) if you have
not picked a pattern yet.

## What you get by default

Every `CliRun` call is already guarded:

- **Bounded waits.** A two-minute default timeout. Reaching the threshold
  attempts a graceful interrupt and, if the child does not exit within a short
  grace period, force-terminates it.
- **Deadlock-free buffered capture.** Buffered mode drains standard output and
  standard error concurrently with the exit wait, so chatty children cannot
  deadlock you. A regression test guards this behaviour.
- **Output caps.** Buffered capture truncates per stream instead of buffering
  without limit.
- **Predictable cancellation.** Cancellation produces a normal result with
  `Canceled = true` rather than an exception, unless you opt in.
- **Pre-start validation.** Invalid configurations throw before a process
  handle is created.

The rest of this guide turns each of these into deliberate choices.

## The failure modes, mapped

| What kills automation | The guard | Where to tune |
|---|---|---|
| Child hangs forever | Timeout policy: interrupt, then force-kill | `timeoutTimeSpan` |
| Host shuts down mid-run | Cancellation contract: `Canceled` result, or OCE | `CancellationToken`, `cancellationThrowsException` |
| Chatty child eats memory | Per-stream buffer cap | `maxBufferedOutputBytes`, `UseOutputTruncation` |
| Nonzero exit silently ignored | Unexpected-exit exceptions, post-exit validation | `ProcessExitConfiguration`, `UsePostExitValidation` |
| Flaky tool retried with lost stdin | Retry middleware replays seekable stdin | `UseRetryPolicy` |
| Secrets leak into logs | Redaction middleware, argument hygiene | `UseLogging` redactor |
| Malformed command line fails late | Length check before launch, unmodified argument lists | free |

## 1. Every call is bounded

The baseline needs no configuration at all:

```csharp
using CliInvoke;
using CliInvoke.Core;

// Two-minute timeout, interrupt-then-kill, buffered capture, truncation cap:
// all default.
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "git",
    arguments: "status --porcelain",
    workingDirectory: repoPath,
    cancellationToken: ct);

if (result.Canceled)
{
    // Timeout or host cancellation. The result carries the kill exit code and
    // no exception was thrown.
}
```

Tighten the timeout per tool class. A formatting step that normally takes two
seconds should not be allowed two minutes:

```csharp
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "npm",
    arguments: "run build",
    timeoutTimeSpan: TimeSpan.FromMinutes(10),
    maxBufferedOutputBytes: 4_000_000,
    cancellationToken: ct);
```

**The timeout contract.** With the default graceful exit configuration,
reaching the threshold returns a normal result with `Canceled = true` and the
kill exit code. Nothing throws. Check `result.Canceled` and treat it as a
failed tool invocation.

**Platform caveat.** The graceful interrupt is best-effort on Windows:
delivering it requires attaching to the child's console, which typically fails
when your own process is a console application. Expect force-termination on
Windows. On Unix the interrupt sends SIGTERM and is genuinely graceful. Either
way the child exits within the grace period or is killed.

## 2. Wire cancellation to host lifetime

Always pass a token, and pick the contract deliberately:

```csharp
using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
    hostApplicationLifetime.ApplicationStopping);

// Default contract: cancellation returns a normal result with Canceled = true
// and empty captured output.
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "dotnet",
    arguments: "test",
    cancellationToken: cts.Token);
```

If you want `OperationCanceledException` instead, opt in on the exit
configuration:

```csharp
ProcessExitConfiguration exitConfig = new(
    ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(5)),
    cancellationThrowsException: true);
```

Note the scope of that knob: when it is on, any cancellation-driven exception
propagates, including the timeout's, because the timeout is implemented as
cancellation. Harness authors usually want the result form so they can record
the kill and keep going. Interactive tools usually want the exception form.

## 3. Capture output without deadlocks or runaway memory

Buffered mode is the safe path: the pipeline drains both streams concurrently
while waiting for exit, which is exactly the shape the classic
`ReadToEnd`-before-`WaitForExit` deadlock punishes. The cap keeps a child that
emits gigabytes of output from taking your process with it, applied
independently to standard output and standard error:

```csharp
BufferedProcessResult result = await CliRun.RunBufferedAsync(
    "ffmpeg",
    arguments: "-i input.mkv output.mp4",
    maxBufferedOutputBytes: 2_000_000,
    cancellationToken: ct);
```

In DI pipelines, `UseOutputTruncation()` provides the equivalent at the
middleware layer. See the [Middleware guide](middleware.md).

**Raw-mode caveat.** Until the 3.2.0 raw-mode normalization ships, avoid raw
mode with `OutputRedirection = true`: raw mode waits for exit without draining
redirected pipes, which deadlocks with chatty children. Buffered mode, or raw
mode with redirection off, are the safe combinations.

## 4. Validate exits instead of ignoring them

The exit-configuration default depends on how you invoke, and the difference
is deliberate:

- The `CliRun` convenience overloads default to `CreateGraceful()`, which
  suppresses unexpected-exit exceptions and returns the result for you to
  inspect.
- A `ProcessExitConfiguration` you construct yourself defaults to
  `AllowExceptionsIfUnexpected`: an unexpected exit throws instead of
  returning a failed result.
- Through the DI-registered `IProcessInvoker`, a null exit configuration
  follows the constructor default and allows unexpected-exit exceptions.

```csharp
var config = new ProcessConfiguration
{
    TargetFilePath = "docker",
    ArgumentList = ["compose", "up", "-d"],
};

// Constructor default: unexpected exits throw instead of returning a result.
ProcessExitConfiguration strict = new(
    ProcessTimeoutPolicy.FromTimeSpan(TimeSpan.FromMinutes(3)));

await CliRun.RunAsync(config, strict, ct);
```

For rules beyond "the exit code was not what we expected", register
`UsePostExitValidation` with your own `IProcessResultValidator<ProcessResult>`.
Agents should treat unexpected exits as tool failures, because an unchecked
nonzero exit code is how automation convinces itself that deployment worked.

## 5. Retry flaky tools safely

Retry middleware composes with everything above:

```csharp
services.AddCliInvoke(builder =>
{
    builder.UseLogging()
           .UseRetryPolicy()      // retries non-zero exit codes by default
           .UseOutputTruncation()
           .UsePostExitValidation(myValidator);
});
```

Two rules make retries safe:

1. **Standard input is replayed on every attempt.** The configured stdin
   stream must be seekable or null; a non-seekable source throws
   `InvalidOperationException` at retry time instead of silently feeding the
   second attempt an empty pipe.
2. **Only retry idempotent commands.** A retried `git push` or
   `nuget push` can double-apply. Retry reads and builds; gate writes behind
   your own idempotency checks.

`RetryOptions` controls attempts, base delay, and the backoff strategy.

## 6. Keep secrets out of logs and command lines

`UseLogging` redacts argument values following `--password`, `--token`, and
`--api-key` flags, and the redaction spans a re-tokenized argument list, so
shell-wrapped invocations are covered too. Supply your own
`Func<string?, string>` redactor if you use a central secret taxonomy such as
`Microsoft.Extensions.Compliance.Redaction`.

One layer earlier: do not put secrets on the command line at all. On Linux,
`/proc/<pid>/cmdline` is world-readable while a process environment is
readable only by its owner, so a token passed as an argument is visible to
every local user for the lifetime of the child. Pass secrets through
environment variables, standard input, or CliInvoke's `UserCredential` and
`SecureString` support instead.

## 7. Fail fast on impossible configurations

Several mistakes become exceptions before a process handle exists:

- **Windows command-line length.** When the assembled command line exceeds the
  `CreateProcess` limit of roughly 32,767 characters, you get an
  `ArgumentException` before launch instead of the opaque Win32 error 206.
  The check is skipped when `UseShellExecute` is enabled.
- **Redirected fire-and-forget.** `CliRun.FireAndForget(ProcessConfiguration)`
  throws when the configuration redirects output or stdin, because nobody
  drains the pipes. Redirected fire-and-forget is broken by construction.
- **Unresolvable targets.** Target file resolution failures throw
  `FileNotFoundException` at construction time, not mid-invocation.

Fail-fast beats a hung child at 3am. Let the library throw early rather than
catching these to keep running with a configuration that cannot work.

## 8. Mind child-process encodings

CliInvoke decodes child output as UTF-8 by default, which is correct for
modern tools. Older Windows tools emit OEM or ANSI code-page text, and
decoding that as UTF-8 produces mojibake. When you know a tool emits legacy
code-page output, set its encodings explicitly in the configuration. See the
[troubleshooting guide](troubleshooting.md) for the detection steps.

## Anti-patterns

- **`FireAndForget` with redirected output or stdin.** It throws by design;
  a fire-and-forget child must not need drained pipes.
- **Raw mode with `OutputRedirection = true`** before the 3.2.0
  normalization. This is the one remaining deadlock shape.
- **Retrying non-idempotent commands.** Pushes and publishes can
  double-apply.
- **Secrets in `Arguments`.** Command lines are world-readable on Linux.
- **Interactive shells from agents.** Omit `-NonInteractive` and a profile
  script or prompt can hang the invocation; CliInvoke's `UseDefaultShell`
  middleware wraps PowerShell with `-NoProfile -NonInteractive -Command` for
  exactly this reason.
- **Disposing caller-owned resources too early.** `StandardInput` and
  `UserCredential` remain yours; dispose them after the invocation completes.
  See [Resource Disposal](resource-disposal.md).

## Where to go next

- [Configuration](configuration.md) for the full reference on every
  configuration model and its defaults.
- [Middleware](middleware.md) for the pipeline contract, `MiddlewareContext`
  usage, and the result-ownership rules.
- [Results](results.md) for result types and the validation rules behind
  unexpected-exit exceptions.
- [Troubleshooting](troubleshooting.md) for symptom-organised diagnosis when
  an invocation misbehaves anyway.
