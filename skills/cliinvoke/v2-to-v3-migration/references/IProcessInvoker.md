# IProcessInvoker Migration Reference

This reference maps v2-style `IProcessInvoker` / `ProcessInvoker`
patterns to v3 replacements. It mirrors the [`IProcessInvoker` Users walkthrough](../../../../site/docs/migration-guides/3.0.0.md#iprocessinvoker-users) in the migration guide.

## ProcessInvoker constructor changes

The two partial constructor overloads were removed. The surviving
constructors are:

- `ProcessInvoker(IExternalProcessFactory)`
- `ProcessInvoker(IExternalProcessFactory, IEnumerable<IProcessMiddleware>, MiddlewareItems?)`

### Before (v2-style)

```csharp
// v2: ProcessInvoker with two-arg constructor
ProcessInvoker invoker = new ProcessInvoker(factory, middleware);
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
    new ProcessConfiguration("dotnet", "--version"));
```

### After (v3)

```csharp
// v3: ProcessInvoker requires three arguments
ProcessInvoker invoker = new ProcessInvoker(factory, middleware, null);
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(
    new ProcessConfiguration("dotnet", "--version"));
```

## DI registration unchanged

`AddCliInvoke` (namespace `CliInvoke.Extensions`) still registers
`IProcessInvoker` and all core services. No changes to DI setup:

```csharp
services.AddCliInvoke();
```

## Middleware via fluent extensions

In v3, use the fluent `Use*` extension methods instead of constructing
the middleware chain manually:

```csharp
using CliInvoke;
using CliInvoke.Extensions;

// Fluent middleware composition
IProcessInvoker invoker = new ProcessInvoker(factory)
    .UseLogging()
    .UsePostExitValidation(PostExitValidation.ExitCodeIsZero());
```

Available middleware extensions:

| Extension | Package | Purpose |
|-----------|---------|---------|
| `UseLogging()` | `CliInvoke` | Logs process entry/exit and output |
| `UsePostExitValidation(...)` | `CliInvoke` | Validates result after exit |
| `UsePowerShell()` | `CliInvoke.Specializations` | Wraps command in PowerShell Core |
| `UseCmd()` | `CliInvoke.Specializations` | Wraps command in Windows `cmd.exe` |

## Removed constructors

| Removed | Replacement |
|---------|-------------|
| `ProcessInvoker(factory, MiddlewareItems?)` | `ProcessInvoker(factory, Array.Empty<IProcessMiddleware>(), items)` |
| `ProcessInvoker(factory, IEnumerable<IProcessMiddleware>)` | `ProcessInvoker(factory, middleware, items: null)` |

> These two partial overloads were the only two-arg forms. The surviving
> constructors are `ProcessInvoker(IExternalProcessFactory)` and
> `ProcessInvoker(IExternalProcessFactory, IEnumerable<IProcessMiddleware>, MiddlewareItems?)`.

## DI `configure` bindings

The `configure` lambda in `AddCliInvoke` now uses the four-argument
`ProcessInvoker` constructor with `sharedItems: null`:

```csharp
services.AddCliInvoke(builder => builder.UseMiddleware<LoggingMiddleware>());
```

## Cross-references

- [Configuration guide](../../../../site/docs/guides/configuration.md) — consumer reference
- [Migration guide — ProcessInvoker constructors](../../../../site/docs/migration-guides/3.0.0.md#4-processinvoker-has-two-constructors)
- [Architecture guide](../../../../site/docs/guides/architecture.md)
