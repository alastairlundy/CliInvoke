---
title: Middleware
layout: simple
---

# Middleware

CliInvoke's `ProcessInvoker` supports an optional **middleware** system that lets you plug cross-cutting concerns (logging, validation, platform selection, retries, …) around the terminal process pipeline without changing how you call it. The pipeline remains the "leaf" that actually starts and waits on the process; middleware wraps it in the order you register.

## When to use middleware, and the two constructors

`ProcessInvoker` has two constructors:

```csharp
// 1. No middleware — the classic, unchanged behavior.
public ProcessInvoker(IExternalProcessFactory externalProcessFactory);

// 2. With middleware — every invocation runs through the chain, in order,
//    before the terminal pipeline executes.
public ProcessInvoker(
    IExternalProcessFactory externalProcessFactory,
    IEnumerable<IProcessMiddleware> middlewares);
```

The full constructor accepts an optional `MiddlewareItems? sharedItems` parameter to seed the per-chain item bag with pre-injected services (such as an `ILogger`). This is how middleware like `LoggingMiddleware` receives a logger at runtime:

```csharp
using CliInvoke.Core.Middleware; // MiddlewareItems

var items = new MiddlewareItems();
items.Set("Logger", myLogger);
var invoker = new ProcessInvoker(factory, Array.Empty<IProcessMiddleware>(), items);
```

Middleware is configured through the `IProcessMiddlewareBuilder` (see [Configuring middleware through DI](#configuring-middleware-through-di) below). Use the factory-only constructor when you don't need middleware. Use the full constructor (factory, an `IEnumerable<IProcessMiddleware>` sequence, and an optional `sharedItems`) when you want logging, validation, or platform wrapping applied to every invocation. Call sites are identical either way: `ExecuteAsync`, `ExecuteBufferedAsync`, and `ExecutePipedAsync` are unchanged.

## The `IProcessMiddleware` contract

A middleware is any `IProcessMiddleware` implementation. It receives the `InvocationContext` and a `next` delegate; calling `next` continues the chain (or the terminal pipeline), omitting it short-circuits:

```csharp
public interface IProcessMiddleware
{
    Task InvokeAsync(
        InvocationContext context,
        Func<InvocationContext, CancellationToken, Task> next);
}
```

Middleware read and share data through `InvocationContext.Middleware.Items` (a typed `MiddlewareItems` bag). For example, `LoggingMiddleware` resolves an `ILogger` from that bag under the well-known key `"Logger"`.

## Built-in middleware

The public API is the **builder extension methods** (`UseLogging`, `UsePostExitValidation`, `UsePowerShell`, `UseCmd`), not the middleware classes (which are internal). These extensions are defined on `IProcessMiddlewareBuilder` and are used when configuring middleware through DI or the builder:

```csharp
using CliInvoke;
using CliInvoke.Extensions.Middleware;            // UseLogging
using CliInvoke.Extensions.Middleware.Validation; // UsePostExitValidation
using CliInvoke.Specializations.Middleware;        // UsePowerShell, UseCmd

builder.Services.AddCliInvoke(builder =>
{
    builder.UseLogging();
    builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
    builder.UsePowerShell();
});
```

* `UseLogging` — logs process entry and exit at `Information`, and each captured stdout/stderr line at `Debug` (when using `BufferedProcessResult`). Sensitive flags (`--password`, `--token`, `--api-key`) are redacted automatically. If no `ILogger` is supplied via the middleware items, a no-op logger is used.
* `UsePostExitValidation(validator)` — runs a validator built from CliInvoke's `CommonValidationRules` against the `ProcessResult` and throws `ProcessValidationException` (with a per-rule failure message) when it fails. Helpers: `PostExitValidation.ExitCodeIsZero()`, `ExitCodeIs(code)`, `ExitCodeIsOneOf(codes...)`, `StdoutMatches(regex)`, `StderrIsEmpty()`.
* `UsePowerShell` / `UseCmd` — rewrite the configuration so the original command executes inside `pwsh` (or `pwsh.exe` on Windows) using `-NoProfile -NonInteractive -Command`, or inside `cmd.exe` using `/c`. `UsePowerShell` also has an overload `UsePowerShell(windowCreation, useShellExecution)` for non-default behaviour; the parameterless form defaults both to `false`, matching the unified defaults used by `PowershellProcessInvoker`, `PowerShellMiddleware` and `ProcessConfiguration`. `UseCmd` is Windows-only and throws `PlatformNotSupportedException` on other platforms; the platform-restricted behaviour mirrors `CmdProcessInvoker`.

## Configuring middleware through DI

Middleware does not need to be wired by hand when you register CliInvoke through `Microsoft.Extensions.DependencyInjection`. The `AddCliInvoke(IServiceCollection, Action<IProcessMiddlewareBuilder>, ServiceLifetime)` overload in `CliInvoke.Extensions.DependencyInjection.DependencyInjectionExtensions` accepts a callback that receives an `IProcessMiddlewareBuilder` and configures the middleware pipeline:

```csharp
using CliInvoke;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware;
using CliInvoke.Extensions.Middleware.Validation;

builder.Services.AddCliInvoke(configure: builder =>
{
    builder.UseLogging();
    builder.UsePostExitValidation(
        new ProcessResultValidator<ProcessResult>(
            [CommonValidationRules<ProcessResult>.RequiresExitCodeZero]));
});
```

The overload works for all three supported lifetimes (`Singleton`, `Scoped`, `Transient`). The `IProcessMiddlewareBuilder` creates the middleware chain from the container's services; the middleware itself still resolves its per-invocation dependencies (for example `ILogger` via the `MiddlewareItems` bag) from the active scope, so DI-driven configuration does not bypass the middleware contract described above.

## Result-ownership and disposal through the chain

Middleware does **not** dispose the process result — the result is returned to you un-disposed, exactly as with a non-middleware invoker. You remain responsible for disposing `PipedProcessResult` (and its streams) and the `ProcessConfiguration` you created. See **[Resource Disposal](resource-disposal.md)** for the full ownership rules and checklist.

## The result-swap rule

By default, middleware does **not** mutate the `ProcessResult` object. Logging and post-exit validation pass the result through unchanged. Platform-selection middleware (`UsePowerShell` / `UseCmd`) substitutes the result of the wrapped `pwsh` / `cmd.exe` invocation — the caller still sees a normal `ProcessResult`, but the data comes from the wrapped shell, not from the original command. Transforming or replacing the result is a deliberate, niche operation: a middleware that does so should write the new result onto `InvocationContext.Result` so the caller receives it.
