# CliRun Migration Reference

This reference maps v2-style `CliRun` static-call patterns to v3
replacements. It mirrors the [`CliRun` Static-Call Users walkthrough](../../../site/docs/migration-guides/3.0.0.md#clirun-static-call-users) in the migration guide.

## CliRun is now stateless

`CliRun.UseExternalProcessFactory` and `CliRun.UseFilePathResolver`
have been removed along with all backing static state. Every
`Run*`/`FireAndForget` call allocates a fresh `ProcessInvocationPipeline`
per call.

### Before (v2-style)

```csharp
// v2: CliRun with factory/resolver configuration
CliRun.UseExternalProcessFactory(myFactory);
CliRun.UseFilePathResolver(myResolver);
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
```

### After (v3)

```csharp
// v3: CliRun is stateless — use IProcessInvoker for custom factories
IProcessInvoker invoker = new ProcessInvoker(myFactory);
ProcessResult result = await invoker.ExecuteAsync(
    new ProcessConfiguration("dotnet", "--version"));
```

## Using CliRun without custom factories

If you were using `CliRun` with the default factory/resolver (no
`Use*` calls), the API is unchanged:

```csharp
// v3: same as before — CliRun works identically for the default case
ProcessResult result = await CliRun.RunAsync("dotnet", "--version");
BufferedProcessResult output = await CliRun.RunBufferedAsync("dotnet", "--info");
int pid = CliRun.FireAndForget("dotnet", "build");
```

## When to use IProcessInvoker instead

Use `IProcessInvoker` (via DI or direct construction) when you need:

- A custom `IExternalProcessFactory`
- A custom `IFilePathResolver`
- Middleware (logging, retry, validation)
- Testability (mock the invoker)

```csharp
// DI registration
services.AddCliInvoke();

// Or direct construction
IProcessInvoker invoker = new ProcessInvoker(myFactory);
```

## Removed APIs

| Removed | Replacement |
|---------|-------------|
| `CliRun.UseExternalProcessFactory(factory)` | Use `IProcessInvoker` via DI or construct `ProcessInvoker` directly |
| `CliRun.UseFilePathResolver(resolver)` | Use `IProcessInvoker` via DI or construct `ProcessInvoker` directly |
| All static backing state | Per-call allocation — no process-wide state |

## Cross-references

- [ADR 0003 — CliRun defaults facade](../../../docs/adr/0003-cli-run-defaults-facade.md)
- [Migration guide — CliRun](../../../site/docs/migration-guides/3.0.0.md#1-clirun-is-now-stateless)
- [Choosing your Invocation Pattern](../../../site/docs/guides/choosing-invocation-pattern.md)
