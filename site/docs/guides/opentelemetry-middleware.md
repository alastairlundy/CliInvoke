---
title: OpenTelemetry tracing middleware
layout: simple
---

# OpenTelemetry tracing middleware

When automation shells out, traces answer questions logs struggle with: which
subprocess consumed the forty seconds, which tool hangs in production, how
often timeouts fire per tool. CliInvoke does not ship an OpenTelemetry
package. The middleware contract makes one unnecessary:
[IProcessMiddleware](middleware.md) is a first-class pipeline participant, so
a span per invocation is about thirty lines of your own code. This recipe is
the starting point.

## The middleware

```csharp
using System.Diagnostics;

using CliInvoke.Core;
using CliInvoke.Core.Middleware;

public sealed class OpenTelemetryMiddleware(ActivitySource? source = null)
    : IProcessMiddleware
{
    // Listen for this source in your OpenTelemetry configuration below.
    private readonly ActivitySource _source = source ?? new ActivitySource("CliInvoke");

    public async Task InvokeAsync(InvocationContext context, Func<InvocationContext, Task> next)
    {
        using Activity? activity = _source.StartActivity(
            $"process {context.Configuration.TargetFilePath}");

        activity?.SetTag("cliinvoke.mode", context.Mode.ToString());

        await next(context);

        if (context.Result is not ProcessResult result)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "invocation produced no result");
            return;
        }

        activity?.SetTag("cliinvoke.exit_code", result.ExitCode);
        activity?.SetTag("cliinvoke.canceled", result.Canceled);
        activity?.SetTag("cliinvoke.duration_ms", result.RuntimeDuration.TotalMilliseconds);
        activity?.SetTag("process.pid", result.ProcessId);

        if (result.Canceled)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "timeout or cancellation");
        }
        else if (result.ExitCode != 0)
        {
            activity?.SetStatus(ActivityStatusCode.Error, $"exit code {result.ExitCode}");
        }
    }
}
```

Three design choices worth keeping:

- **No output content in spans.** Standard output and error belong in your
  logs, not in trace attributes; putting them on a span leaks secrets into
  every exporter and bloats cardinality. Arguments are absent for the same
  reason: command lines can carry tokens, and `UseLogging`'s redaction is the
  right place for them if you need them anywhere at all.
- **Low-cardinality tags.** Mode, exit code, and the canceled flag let
  dashboards roll up per tool, which is what answers "which tool eats the
  forty seconds" and "what is our timeout rate".
- **Zero overhead when tracing is off.** `StartActivity` returns null when no
  listener subscribes to the source, so every line here is a no-op unless
  OpenTelemetry is active.

## Register it

```csharp
services.AddCliInvoke(builder =>
{
    builder.UseMiddleware(new OpenTelemetryMiddleware());
    builder.UseLogging();
});
```

Place it first when you want the span to wrap the full chain, including
logging, retry, and validation middleware. A retried invocation appears as
one span containing the whole attempt loop; if you need per-attempt spans,
nest `ActivitySource.StartActivity` inside a middleware placed below
`UseRetryPolicy` in the chain.

## Listen for the source

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("CliInvoke")
        .AddOtlpExporter());
```

Packages: `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Trace`, and
`OpenTelemetry.Exporter.OpenTelemetryProtocol`.

## What you get

One span per invocation, named `process <target>`, with exit code, duration,
mode, and the canceled flag as tags. In a trace waterfall that is the
subprocess-cost lane people otherwise reconstruct by grepping timestamps, and
in a dashboard it is p95 duration and timeout rate per external tool.

## Where to go next

- [Middleware](middleware.md) for the full contract: chain order,
  `MiddlewareContext`, result ownership, and DI resolution.
- [Guarded execution for automation and agents](guarded-execution.md) for the
  guards these spans will measure.
- [Troubleshooting](troubleshooting.md) when a traced invocation misbehaves.
