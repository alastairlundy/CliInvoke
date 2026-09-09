---
name: v2-to-v3-migration
description: Maps v2-style code to v3 replacements across all three invocation patterns and construction. USE FOR detecting and migrating v2-style code in CliInvoke projects. DO NOT USE FOR writing new v3 code from scratch without v2 references.
compatibility: Requires one or more CliInvoke NuGet packages (CliInvoke.Core, CliInvoke, CliInvoke.Specializations)
---

# CliInvoke v2 to v3 Migration

## When to Use

- When migrating an existing v2-based CliInvoke project to v3.
- When auditing a codebase for v2-style patterns that need to be updated.
- When encountering removed APIs (`ProcessConfigurationFactory`, `CliRun.UseExternalProcessFactory`, `ExitConfiguration` setter, `ArgumentsList`, `PipedProcessResult`).
- When the user's code defaults to v3-advanced construction styles (reaching for `ProcessConfigurationBuilder` by habit where init construction is the v3 default).

## When not to use

- When writing new v3 code from scratch with no v2 references — the patterns in v3 are self-describing.
- When the question is about general CliInvoke usage, configuration, or execution — load `select-execution-pattern` instead.
- When the project has no v2 dependency and the task is unrelated to migration.

## v2-style code detection

**v2-style code** is defined in [GLOSSARY.md](../../../../GLOSSARY.md) as:

> Code that a v3 migration must change: (a) it uses APIs of the prior major version (v2) that v3 removed or changed, or (b) it defaults to v3-advanced construction styles — reaching for `ProcessConfigurationBuilder` by habit where init construction is the v3 default.

### Detection signals

Scan for these patterns to identify v2-style code:

| Signal | What to look for |
|--------|------------------|
| `ProcessConfigurationFactory` | Removed in v3 — construct `ProcessConfiguration` directly |
| `CliRun.UseExternalProcessFactory(...)` | Removed — use `IProcessInvoker` or DI |
| `CliRun.UseFilePathResolver(...)` | Removed — use `IProcessInvoker` or DI |
| `process.ExitConfiguration = ...` | Setter removed — pass at construction |
| `ArgumentsList` property | Renamed to `ArgumentList` |
| `PipedProcessResult` / `ExecutePipedAsync` / `RunPipedAsync` | Removed — use `IExternalProcess` for streaming |
| `ProcessInvoker(factory, middleware)` | Two-arg constructor removed — use three-arg form |
| `ExternalProcess(resolver, "path")` | Two-arg constructor removed — use constructor C |
| `new ProcessConfigurationBuilder()` as default | Use init construction instead; builder is advanced-only |
| Subclassing `ExternalProcess` | Sealed in v3 |
| Subclassing `ProcessConfigurationBuilder` | Sealed in v3 |

### Deliberate advanced-builder usage

The following uses of `ProcessConfigurationBuilder` are **not** v2-style:

- Argument escaping (arguments containing `\`, `"`, or control characters)
- `UserCredentialSpec` for Windows-domain credential staging
- `ProcessResourcePolicySpec` for resource-policy callback flows

## The three-pattern partition

The migration references mirror the guide's walkthrough structure. Load
the appropriate reference based on which pattern the v2-style code uses:

| Pattern | Reference | Guide walkthrough |
|---------|-----------|-------------------|
| Construction (all patterns) | [Construction.md](references/Construction.md) | [Shared Construction](../../../site/docs/migration-guides/3.0.0.md#shared-construction-v3-default) |
| `CliRun` static calls | [CliRun.md](references/CliRun.md) | [`CliRun` Static-Call Users](../../../site/docs/migration-guides/3.0.0.md#clirun-static-call-users) |
| `IProcessInvoker` / `ProcessInvoker` | [IProcessInvoker.md](references/IProcessInvoker.md) | [`IProcessInvoker` Users](../../../site/docs/migration-guides/3.0.0.md#iprocessinvoker-users) |
| `IExternalProcess` / `ExternalProcess` | [IExternalProcess.md](references/IExternalProcess.md) | [`IExternalProcess` Users](../../../site/docs/migration-guides/3.0.0.md#iexternalprocess-users) |

## Migration strategy

1. **Scan** — grep for the detection signals above.
2. **Classify** — determine which pattern the code uses (`CliRun`, `IProcessInvoker`, or `IExternalProcess`).
3. **Apply** — load the corresponding reference and apply the Before/After mapping.
4. **Verify** — check that no v2-era identifiers remain.

## Common pitfalls

| Pitfall | Solution |
|---------|----------|
| Using `ProcessConfigurationFactory` | Construct `ProcessConfiguration` directly with init setters or the convenience constructor |
| Using `CliRun.UseExternalProcessFactory()` | Switch to `IProcessInvoker` via DI or construct `ProcessInvoker` directly |
| Using `ExitConfiguration` setter | Pass `ProcessExitConfiguration` to the `ExternalProcess` constructor |
| Using `ArgumentsList` | Rename to `ArgumentList` and use init setter |
| Using `PipedProcessResult` / piped methods | Use `IExternalProcess` for streaming output |
| Builder as default construction | Use init construction; reserve builder for escaping/credentials/policy |

This is a pure knowledge skill and does not invoke external tools.
