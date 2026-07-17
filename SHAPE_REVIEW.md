# Critical Evaluation — CliRun Shape Decision Ledger

I read the ledger, the source files it touches (`CliRun.cs`, `ExternalProcessFactory.cs`, `ProcessConfiguration.cs`, `ProcessConfigurationFactory.cs`, `ExternalProcess.cs`), and the two F1/F2 ledgers it cross-cites. Below are the weaknesses I see, grouped by severity.

## High Severity

### 1. T002's lazy default factory breaks `UseFilePathResolver` for the default path
**The current code** constructs the factory as `new ExternalProcessFactory(resolver)` where `resolver = GetFilePathResolver()` (`CliRun.cs:133-135`). This means `UseFilePathResolver(custom)` is honored even if the caller never calls `UseExternalProcessFactory`.

**The ledger's plan** (`T002`) initializes the default delegate as `() => _defaultFactory.Value`, where `_defaultFactory` is `new ExternalProcessFactory()` — the parameterless ctor. That ctor internally allocates its own `new FilePathResolver()` (`ExternalProcessFactory.cs:30-33`) and has no access to the static `_filePathResolver`.

**Result:** if a caller only sets a custom resolver via `UseFilePathResolver(customResolver)`, the default factory path will ignore it entirely. `UseFilePathResolver` becomes a no-op unless the caller also supplies a matching factory. This is a behavior regression, and it contradicts the current working behavior of the code.

**Fix:** the default delegate should be `() => new ExternalProcessFactory(GetFilePathResolver())`, or `_defaultFactory` should be initialized with the resolver at first access. The mandated comment ("constructed on first access; do not reset") should not be allowed to hide this bug.

### 2. Resolver precedence is inconsistent between string-arg and config-arg overloads
`T001` says `BuildStringArgsConfig` shall call `GetFilePathResolver()` and eagerly write the resolved `FullName` into `ProcessConfiguration.TargetFilePath`. But:

- For **string-arg overloads**, resolution is performed by the static `GetFilePathResolver()`, regardless of which factory is registered.
- For **config-arg overloads**, no pre-resolution happens; the factory's own resolver resolves the path inside `StartAsync`.

This creates four possible behaviors:
| `UseFilePathResolver` | `UseExternalProcessFactory` | String overload | Config overload |
|---|---|---|---|
| custom | default | static custom resolver resolves, factory's internal default resolver is skipped | factory's internal default resolver resolves |
| default | custom factory with custom resolver | static default resolver resolves, factory's custom resolver is skipped | factory's custom resolver resolves |
| custom | custom factory with *different* custom resolver | static custom resolver wins, factory's resolver skipped | factory's custom resolver wins |

The seam is therefore **not symmetric**. A caller who mixes the two configuration APIs will get resolution from one source for string overloads and a different source for config overloads. The ledger acknowledges the stack-trace shift but does not acknowledge this semantic split.

**Fix:** either make string overloads route through the registered factory's resolver (i.e., resolve via the factory, not via the static getter), or document explicitly that `UseFilePathResolver` only affects the default factory and that custom factories fully own resolution.

### 3. T003's test cannot actually verify the lazy default factory is not constructed
The ledger says tests should assert `(c) the custom factory's CreateExternalProcess is invoked exactly once per Run*Async call (verifying the lazy default factory is not constructed when a custom factory is registered).`

This is a **logical non-sequitur**. Asserting that the custom factory was called exactly once does **not** prove the lazy default was never constructed. The default `Lazy<T>` could construct its `ExternalProcessFactory` on a background thread or as a side effect and simply not be used. To verify T002, the test would need to observe the private `_defaultFactory` field (reflection or `InternalsVisibleTo`) or make the default factory throw on construction and assert no throw.

**Fix:** rewrite the claim. Either test only that the custom factory is used, or add a real negative test for lazy construction (e.g., a custom resolver/factory setup that would fail if the default factory's parameterless ctor ran).

---

## Medium Severity

### 4. D004's named tuple is fragile
The ledger explicitly notes: "The tuple element names (`Configuration`, `ExitConfiguration`) travel at the call site, not in the helper's signature; a future rename in the helper's tuple definition can drift from the call-site destructures unless a unit test exercises the end-to-end flow."

That is exactly the weakness. A private nested `record` or `readonly struct` (e.g., `StringArgsConfig`) would be self-documenting, type-safe, and rename-safe. Named tuples in a private helper are a code smell here because the names do not propagate across the method boundary and the type has no identity.

### 5. Tuple destructure breaks the `using` declaration ergonomics
Currently each string overload uses:

```csharp
using ProcessConfiguration configuration = ProcessConfigurationFactory.Create(...);
```

After D004, the code becomes:

```csharp
var (configuration, exitConfiguration) = BuildStringArgsConfig(...);
// now dispose configuration separately before/after the await
```

You cannot write `using var (configuration, exitConfiguration) = ...`. You must either wrap the call in a nested `using` block or use a separate `using var configuration = ...` after the destructure. This is awkward and increases the chance that a future edit forgets to dispose the configuration.

### 6. Static mutable state + parallel test execution is not addressed
`CliRun` is a `public static class` with mutable static fields. TUnit defaults to running tests in parallel. The `[BeforeEach]` reset strategy described in `T003` only works reliably if tests are **not** run in parallel, or if the entire test class is serialized. If two tests mutate `_externalProcessFactory` / `_filePathResolver` concurrently, they will interfere. The ledger does not mention `[NotInParallel]`, `[ParallelLimiter]`, or any other TUnit mechanism to prevent cross-test races.

### 7. D005/T004 pre-specifies F1 pipeline integration that F1 has not decided
`D005` says: "When F1 ships, a follow-up edit rewrites `RunInternalAsync<T>`'s body to construct a `ProcessInvocationContext` and call `_pipeline.ExecuteAsync(context, capture, cancellationToken)`."

But F1's `D002` only says the pipeline accepts a single `ProcessInvocationContext` and mutates it; it does **not** specify a delegate-based `ExecuteAsync(context, capture, ...)` signature. By hard-coding this signature now, F4 constrains F1's design before F1 is finalized. This is the same pattern that created the dead `_externalProcessFactory` field in the first place: a forward-integration guess that becomes stale.

`T004`'s comment rule points to `ProcessInvocationPipeline`, a type that does not yet exist. Forward-referencing non-existent types in comments is acceptable in a plan, but it is another sign that this integration is under-specified.

### 8. T001 requires amending F2 but assigns no owner
`T001` states: "Requires a follow-up amendment to `DECISIONS-CliInvoke-process-configuration-shape.md` D001 and D013 to allow eager resolution as a legitimate pattern for `CliRun` callers."

This is a real dependency. Without that amendment, F2's `D001` still says `TargetFilePath` is the resolution slot written to at execution time, and `T001` contradicts that for `CliRun`-built configs. The ledger does not say who writes the amendment or whether it blocks F4 implementation.

### 9. The file-path-resolver ADR is missing
The ledger cross-references `docs/decisions/DECISIONS-CliInvoke-file-path-resolver-seam.md` twice (in the header and in `D001`), but that file does not exist in the working tree. A decision ledger should not cite documents that are not present without at least noting their absence.

---

## Low Severity / Observations

### 10. "Public surface is unchanged" is misleading
`D001` says: "The public surface (`UseExternalProcessFactory`, `UseFilePathResolver`, the 6 `Run*Async` methods) is unchanged."

Signatures are unchanged, but **behavior is not**: resolution timing changes (`T001`), the default factory becomes lazy (`T002`), and the dead field is finally honored (`D001`). These are observable changes. The ledger should distinguish "binary/API unchanged" from "behavior unchanged."

### 11. Relying on comments to prevent future refactoring is weak
`T002` mandates a comment "constructed on first access; do not reset," and `T004` mandates a comment pointing to the F1 pipeline. Comments are not enforceable. If the design is subtle enough to need a warning comment, consider making it impossible to misuse instead (e.g., make `_defaultFactory` a property with no setter, or choose a self-evident name).

### 12. The generic funnel delegate is fine but not free
`RunInternalAsync<T>` takes `Func<IExternalProcess, CancellationToken, Task<T>>`. For the three call sites, the lambdas `(p, t) => p.WaitForExitOrTimeoutAsync(t)` etc. are stateless and will be cached by the compiler, so there is no closure allocation. However, the delegate itself is still passed as an argument each call. This is negligible for process-launching APIs but worth confirming during implementation.

### 13. No disposal guidance for test fakes
`T003` proposes custom `IFilePathResolver` and `IExternalProcessFactory` fakes but does not say whether they need to implement `IDisposable` or how they are cleaned up. Since `IExternalProcess` is `IDisposable` and the test fake factory may hold state, this should be specified.

---

## Summary

The ledger is coherent at a high level, but it has **three critical flaws** that could silently change behavior or produce unverifiable tests:

1. **T002's lazy default factory must use the configured resolver**, or `UseFilePathResolver` becomes dead code for default users.
2. **T001 creates an inconsistent resolver-precedence model** between string-arg and config-arg overloads that needs to be resolved explicitly.
3. **T003's test claim is logically invalid** — custom-factory call count does not prove lazy-default non-construction.

On the medium tier, the tuple-return helper, the parallel-test risk, and the premature F1 pipeline signature are the biggest risks. I would not implement this ledger as-is without fixing at least the high-severity items.
