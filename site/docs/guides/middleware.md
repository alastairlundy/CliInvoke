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
//    before the terminal pipeline executes. Pass a MiddlewareItems? to
//    pre-seed the per-chain item bag (e.g. an ILogger); pass null for none.
public ProcessInvoker(
    IExternalProcessFactory externalProcessFactory,
    IEnumerable<IProcessMiddleware> middlewares,
    MiddlewareItems? sharedItems);
```

The full constructor accepts an optional `MiddlewareItems? sharedItems` parameter to seed the per-chain item bag with pre-injected services (such as an `ILogger`). This is how middleware like `LoggingMiddleware` receives a logger at runtime:

```csharp
using CliInvoke.Core.Middleware; // MiddlewareItems

var items = new MiddlewareItems();
items.Set("Logger", myLogger);
var invoker = new ProcessInvoker(factory, Array.Empty<IProcessMiddleware>(), items);
```

Middleware is configured through the `IProcessMiddlewareBuilder` (see [Configuring middleware through DI](#configuring-middleware-through-di) below). Use the factory-only constructor when you don't need middleware. Use the full constructor (factory, an `IEnumerable<IProcessMiddleware>` sequence, and an optional `sharedItems`) when you want logging, validation, or platform wrapping applied to every invocation. Call sites are identical either way: `ExecuteAsync` and `ExecuteBufferedAsync` are unchanged.

## The `IProcessMiddleware` contract

A middleware is any `IProcessMiddleware` implementation. It receives the `InvocationContext` and a `next` delegate; calling `next` continues the chain (or the terminal pipeline), omitting it short-circuits:

```csharp
public interface IProcessMiddleware
{
    Task InvokeAsync(
        InvocationContext context,
        Func<InvocationContext, Task> next);
}
```

The `next` delegate takes only the `InvocationContext`; the `CancellationToken` is available as `context.CancellationToken` (not a separate parameter). Calling `next(context)` continues the chain (or the terminal pipeline); omitting the call short-circuits.

Middleware read and share data through `InvocationContext.Middleware.Items` (a typed `MiddlewareItems` bag). For example, `LoggingMiddleware` resolves an `ILogger` from that bag under the well-known key `"Logger"`.

## Built-in middleware

The public API is the **builder extension methods** (`UseLogging`, `UsePostExitValidation`, `UsePowerShell`, `UseCmd`), not the middleware classes (which are internal). These extensions are defined on `IProcessMiddlewareBuilder` and are used when configuring middleware through DI or the builder:

```csharp
using CliInvoke;
using CliInvoke.Extensions;
using CliInvoke.Extensions.Middleware;            // UseLogging
using CliInvoke.Extensions.Middleware.Validation; // UsePostExitValidation
using CliInvoke.Specializations.Middleware;        // UsePowerShell, UseCmd

builder.Services.AddCliInvoke(builder =>
{
    builder.UseLogging();
    builder.UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
    builder.UsePowerShell();
});
builder.Services.AddCliInvokeSpecializations(); // registers the platform middleware types
```

* `UseLogging` — logs process entry and exit at `Information`, and each captured stdout/stderr line at `Debug` (when using `BufferedProcessResult`). A built-in heuristic redacts the values following the sensitive flags (`--password`, `--token`, `--api-key`); captured stdout/stderr lines are redacted too. To apply an organisation-wide secret taxonomy instead, construct `LoggingMiddleware` with a `Func<string?, string>?` redactor (for example Microsoft's `Microsoft.Extensions.Compliance.Redaction` `IRedactorProvider`) — the built-in heuristic is used when no redactor is supplied. If no `ILogger` is supplied via the middleware items, a no-op logger is used.
* `UsePostExitValidation(validator)` — runs a validator built from CliInvoke's `CommonValidationRules` against the `ProcessResult` and throws `ProcessValidationException` (with a per-rule failure message) when it fails. Helpers: `PostExitValidation.ExitCodeIsZero()`, `ExitCodeIs(code)`, `ExitCodeIsOneOf(codes...)`, `StdoutMatches(regex)`, `StderrIsEmpty()`.
* `UsePowerShell` / `UseCmd` — rewrite the configuration so the original command executes inside `pwsh` (or `pwsh.exe` on Windows) using `-NoProfile -NonInteractive -Command`, or inside `cmd.exe` using `/c`. `UsePowerShell()` is a parameterless extension on `IProcessMiddlewareBuilder` (as is `UseCmd()`); the parameterless form defaults `WindowCreation` and `UseShellExecution` to `false`, matching the unified defaults used by `PowershellProcessInvoker`, `PowerShellMiddleware` and `ProcessConfiguration`. To configure non-default behaviour, register `ShellMiddlewareOptions` (namespace `CliInvoke.Specializations.Middleware`, with `bool WindowCreation` and `bool UseShellExecution` properties) in the DI container — for example:

  ```csharp
  using CliInvoke.Specializations.Middleware; // ShellMiddlewareOptions

  services.Configure<ShellMiddlewareOptions>(o =>
  {
      o.WindowCreation = true;
      o.UseShellExecution = true;
  });
  ```

  Shell wrapping is delivered via the shell middleware on plain `ProcessConfiguration`.
  The `ShellArgumentEscaper` type is no longer public — escaping is an internal concern
  of the `ShellRewriter` composition core. Callers should rely on the middleware or the
  `ShellRewriter` directly for shell-escaped command composition.

### Deprecated subclasses

`PowershellProcessConfiguration` and `CmdProcessConfiguration` are deprecated as of
3.1.0 and will be removed in 4.0. Use plain `ProcessConfiguration` with the
appropriate shell wrapping middleware instead:

```csharp
// Before (deprecated):
var config = new PowershellProcessConfiguration(arguments: "Get-Process");

// After:
var config = new ProcessConfiguration("pwsh", "Get-Process");
// Register UsePowerShell() in the middleware pipeline
```

* `UseDefaultShell` — detects the user's default shell (pwsh, Windows PowerShell, or cmd) via `IShellDetector` and wraps the command in it automatically. Use this instead of `UsePowerShell`/`UseCmd` when you want cross-platform shell detection without committing to a specific shell. Requires `IShellDetector` (registered by `AddCliInvoke`).

  `UseCmd` is Windows-only and throws `PlatformNotSupportedException` on other platforms; the platform-restricted behaviour mirrors `CmdProcessInvoker`.

  The `PowerShellMiddleware`/`CmdMiddleware`/`DefaultShellMiddleware` types behind `UsePowerShell()`/`UseCmd()`/`UseDefaultShell()` are registered in the DI container by `AddCliInvokeSpecializations()` (shipped in the `CliInvoke.Specializations` package). Call it alongside `AddCliInvoke` with the same `ServiceLifetime`; without it, resolving the invoker throws `InvalidOperationException` because the middleware types are not registered.

## Wrapping only some invocations in a shell

`UsePowerShell()` rewrites every invocation that reaches it. Often you want most commands to run directly and only a few to go through PowerShell. There are two ways to do this, and they fit different situations.

### Conditional middleware with `UseWhen`

`IProcessMiddlewareBuilder.UseWhen` takes a predicate over the `InvocationContext` and runs a sub-pipeline only when the predicate returns `true`. When it returns `false`, the invocation skips the sub-pipeline and continues down the chain untouched:

```csharp
using CliInvoke;
using CliInvoke.Extensions;
using CliInvoke.Specializations;             // AddCliInvokeSpecializations
using CliInvoke.Specializations.Middleware;  // UsePowerShell

builder.Services.AddCliInvoke(b =>
{
    b.UseLogging();
    b.UseWhen(
        ctx => ctx.Configuration.TargetFilePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase),
        shell => shell.UsePowerShell());
});
builder.Services.AddCliInvokeSpecializations();
```

With this registration, a configuration targeting a `.ps1` file runs inside `pwsh`. A configuration targeting `dotnet` or `git` runs directly. The predicate is evaluated once per invocation, at the point the chain reaches it, so place `UseWhen` in the order you want the wrapping to happen relative to your other middleware.

An async overload (`Func<InvocationContext, Task<bool>>`) exists for decisions that need I/O, such as reading a feature flag or a policy file. The synchronous overload is cheaper; prefer it when the decision comes from the configuration alone.

This approach works well when the rule can be derived from what you are invoking: the target file path, the arguments, the environment variables. If the decision belongs to the calling code instead, register two invokers.

### Two invoker registrations

`AddCliInvoke` registers one `IProcessInvoker`. Keep that one plain and add a second, keyed registration that carries the shell middleware. Callers then pick the invoker at the injection site:

```csharp
using CliInvoke;
using CliInvoke.Core;
using CliInvoke.Core.Factories;
using CliInvoke.Core.Middleware;
using CliInvoke.Extensions;
using CliInvoke.Specializations;             // AddCliInvokeSpecializations
using CliInvoke.Specializations.Middleware;  // UsePowerShell

// The plain invoker, registered non-keyed as usual.
builder.Services.AddCliInvoke(b => b.UseLogging());
builder.Services.AddCliInvokeSpecializations();

// A second invoker with shell wrapping, registered under a key.
builder.Services.AddKeyedScoped<IProcessInvoker>("shell", (sp, _) =>
{
    IExternalProcessFactory factory = sp.GetRequiredService<IExternalProcessFactory>();
    ProcessMiddlewareBuilder mwBuilder = new(sp);
    mwBuilder.UsePowerShell();
    return new ProcessInvoker(factory, mwBuilder.Build(), null);
});
```

Keyed and non-keyed registrations are independent in `Microsoft.Extensions.DependencyInjection`, so this does not conflict with the registration `AddCliInvoke` made. Consumers choose per dependency:

```csharp
public class ScriptRunner(
    [FromKeyedServices("shell")] IProcessInvoker shellInvoker)
{
    public async Task<BufferedProcessResult> RunScriptAsync(string command, CancellationToken ct = default)
    {
        ProcessConfiguration config = new("pwsh", command);
        return await shellInvoker.ExecuteBufferedAsync(
            config, ProcessExitConfiguration.CreateGraceful(), ct);
    }
}
```

Anything injecting a plain `IProcessInvoker` still gets the un-wrapped one. Both invokers share the same `IExternalProcessFactory` and the same service lifetimes, so behaviour differs only in the middleware chain.

Match the keyed registration's lifetime (`AddKeyedScoped`, `AddKeyedSingleton`, `AddKeyedTransient`) to the lifetime you passed to `AddCliInvoke`. Mismatched lifetimes risk capturing scoped services into a singleton, the same hazard the `AddCliInvokeSpecializations` docs warn about.

If you would rather have *both* invokers keyed, register the plain one yourself under a second key (for example `"plain"`) the same way, and skip the non-keyed registration by registering `AddCliInvoke`'s dependencies manually. Most apps are fine with one plain and one keyed.

### Which one to use

Use `UseWhen` when the rule is a property of the command being run and you can express it as a predicate over the configuration. Use two invoker registrations when the choice is a decision the calling code makes, or when different parts of your app have different shell policies and you want the compiler, not a predicate, to enforce the split. The two compose: a keyed shell invoker can itself use `UseWhen` internally if only a subset of its invocations should wrap.

## Configuring middleware through DI

Middleware does not need to be wired by hand when you register CliInvoke through `Microsoft.Extensions.DependencyInjection`. The `AddCliInvoke(IServiceCollection, Action<IProcessMiddlewareBuilder>, ServiceLifetime)` overload in `CliInvoke.Extensions.AddCliInvokeExtensions` accepts a callback that receives an `IProcessMiddlewareBuilder` and configures the middleware pipeline:

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
            [CommonValidationRules<ProcessResult>.ExitCodeZeroRule()]));
});
```

The overload works for all three supported lifetimes (`Singleton`, `Scoped`, `Transient`). The `IProcessMiddlewareBuilder` creates the middleware chain from the container's services; the middleware itself still resolves its per-invocation dependencies (for example `ILogger` via the `MiddlewareItems` bag) from the active scope, so DI-driven configuration does not bypass the middleware contract described above.

## Result-ownership and disposal through the chain

Middleware does **not** dispose the process result — the result is returned to you un-disposed, exactly as with a non-middleware invoker. You remain responsible for disposing any `UserCredential` or `StreamWriter` you placed inside the `ProcessConfiguration`. See **[Resource Disposal](resource-disposal.md)** for the full ownership rules and checklist.

## The result-swap rule

By default, middleware does **not** mutate the `ProcessResult` object. Logging and post-exit validation pass the result through unchanged. Platform-selection middleware (`UsePowerShell` / `UseCmd`) substitutes the result of the wrapped `pwsh` / `cmd.exe` invocation — the caller still sees a normal `ProcessResult`, but the data comes from the wrapped shell, not from the original command. Transforming or replacing the result is a deliberate, niche operation: a middleware that does so should write the new result onto `InvocationContext.Result` so the caller receives it.
