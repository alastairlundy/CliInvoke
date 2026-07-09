# Implementation Blueprint — File-Path Resolver Seam (PR2)

> **Scope**: PR2 of the file-path resolver seam refactor. PR2 introduces `FilePathResolverBase` in `CliInvoke.Core`, changes the strategy methods to `protected abstract`, applies the renames, adds the `Try*` wrapper, wires the `CliRun` resolver with a lock, and adds the resolution-order documentation. PR1 (separate blueprint) removes `Shared` and updates the ctor defaults.

## Scope Binding

- **Linked Spec**: Architecture-review candidate **C2** (make the file-path resolver seam real) — referenced in the Decision Ledger header.
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-file-path-resolver-seam.md`
- **Authorization notice**: This blueprint is a context pointer valid ONLY for the linked spec and the cited Decision Ledger. It must not be applied to other specifications without explicit authorization.

## Goals

PR2 accomplishes nine things:

1. Introduce `FilePathResolverBase` in `CliInvoke.Core` as a `public abstract class` implementing `IFilePathResolver` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D007`].
2. Add `bool TryResolveFilePath(string, out FileInfo?)` to `IFilePathResolver` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D003`].
3. Declare the two strategy methods on the base as `protected abstract` (currently `protected` on the concrete) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D004`].
4. Declare the two data accessors on the base as `protected virtual` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D004`].
5. Rename `GetPathInfo` → `EnumeratePathDirectories` and `GetPathExtensionsInfo` → `GetPathFileExtensions` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`].
6. Lowercase the extensions in `GetPathFileExtensions` in a single pass at the producer (the base) rather than the consumer (the loop) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`].
7. Add the `Try*` wrapper on the base, catching `Exception` per the .NET `Try*` convention; third-party implementers of `IFilePathResolver` directly (not extending the base) write the wrapper themselves with the documented catch discipline [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T001`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T007`].
8. Wire `CliRun.UseFilePathResolver(IFilePathResolver)` with a lazy default under `lock(_syncRoot)`; if `UseFilePathResolver` has not been called, `CliRun` constructs `new FilePathResolver()` on first `Run*` call and caches it [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D009`].
9. Document the resolution order (PATH first, then directory recursion) in the base's body and in `CONTEXT.md` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].

## Scope of Changes

### 1. `src/CliInvoke.Core/IFilePathResolver.cs`

Add the `TryResolveFilePath` method to the interface [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D003`]:

```csharp
bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath);
```

Update the XML doc to reflect the new method's contract: success sets `resolvedFilePath` to the `FileInfo` and returns `true`; failure sets `resolvedFilePath` to `null` and returns `false` without throwing.

### 2. `src/CliInvoke.Core/FilePathResolverBase.cs` (new file)

Create `public abstract class FilePathResolverBase : IFilePathResolver` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D007`].

The base contains:

- `public FileInfo ResolveFilePath(string filePathToResolve)` — the public algorithm, calling the two `protected abstract` strategies in the documented order (PATH first, then directory recursion) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T001`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`]. Body includes a one-line `// PATH first, then directory recursion — see CONTEXT.md` comment [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].
- `public bool TryResolveFilePath(string filePathToResolve, out FileInfo? resolvedFilePath)` — a wrapper that catches `Exception` (per the .NET `Try*` convention, not `FileNotFoundException`) and returns `false` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T001`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T007`].
- `protected abstract bool ResolveFromPathEnvironmentVariable(string filePathToResolve, out FileInfo? resolvedFilePath)` — the PATH-lookup strategy [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D004`].
- `protected abstract FileInfo LocateFileFromDirectory(string filePathToResolve)` — the directory-recursion fallback [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D004`].
- `protected virtual IEnumerable<string>? EnumeratePathDirectories()` — the data accessor for PATH entries (renamed from `GetPathInfo`; the `Enumerate`-prefix reflects the lazy `IEnumerable<T>?` return type, paralleling the convention in D006) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`].
- `protected virtual string[] GetPathFileExtensions()` — the data accessor for file extensions (renamed from `GetPathExtensionsInfo`; the `Get`-prefix reflects the array return type, paralleling the convention in D006), with lowercasing applied in a single pass before returning [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`].

The base's `ResolveFilePath` body has a one-line `// PATH first, then directory recursion — see CONTEXT.md` comment [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].

### 3. `src/CliInvoke/FilePathResolver.cs`

Change the class declaration to `public class FilePathResolver : FilePathResolverBase` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D007`]. Verify the `Shared` static property is removed (PR1's responsibility, but confirm) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D001`]. Convert the strategy methods from `protected` to `override protected` (implementing the abstract methods on the base). Rename the data accessors to `EnumeratePathDirectories` and `GetPathFileExtensions`, and apply the lowercasing to the latter [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`].

### 4. `src/CliInvoke/Extensions/CliRun.cs`

Add the lock and the lazy-default helper per T014:

- `private static IFilePathResolver? _filePathResolver` (default `null`) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`].
- `private static readonly object _syncRoot = new()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`].
- `public static void UseFilePathResolver(IFilePathResolver resolver)`:
  - Validate the argument (throw `ArgumentNullException` on `null`; do not overwrite the field).
  - Assign the field under `lock(_syncRoot)` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D009`].
- Private `GetFilePathResolver()` helper:
  - Read the field outside the lock for the hot path.
  - If `null`, acquire `lock(_syncRoot)`, double-check the field, and construct `new FilePathResolver()` if still `null` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`].

Modify each `Run*` method (`RunAsync`, `RunBufferedAsync`, `RunPipedAsync` — both string-target and `ProcessConfiguration` overloads, six entry points total) to call `GetFilePathResolver()` and use the returned resolver. The existing `_externalProcessFactory` lambda is unchanged; the lazy default in `CliRun` is a parallel path for users who want the static `CliRun` to use a specific resolver without configuring the factory.

The asymmetric sync pair (lock on `UseFilePathResolver` + `GetFilePathResolver`, no lock on the existing `UseExternalProcessFactory` at line 32) shall be documented in `CONTEXT.md` so reviewers do not "fix" it by removing the lock or by adding a lock to `UseExternalProcessFactory` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014` constraints].

### 5. `CONTEXT.md`

Add a "design decisions" section containing the entries the implementation depends on:

- The resolution order rationale (PATH is cheap, directory recursion is slow; the order is a performance contract) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].
- The convention "Get for arrays, Enumerate for `IEnumerable`" (the asymmetry between `GetPathFileExtensions` and `EnumeratePathDirectories` is intentional, not debt) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`].
- The lowercasing contract for `GetPathFileExtensions` (custom resolvers overriding it must return lowercased extensions; raw extensions cause silent "no match found" bugs) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`].
- The catch discipline for `Try*` methods (`Exception`, not `FileNotFoundException`, per the .NET `Try*` convention; the broader catch is required by convention, not by algorithm) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T001`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T007`].
- The `CliRun` lifetime convention ("lifetime follows the parameter"; the resolver is not special-cased in `AddCliInvoke`) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T008`].
- The asymmetric sync pair note (lock on `UseFilePathResolver`, no lock on `UseExternalProcessFactory`; intentional, not debt) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`].

### 6. Tests

Per T006's strategy, the test suite combines unit tests on `FilePathResolverBase` (a custom subclass overrides the two `protected abstract` strategies with stubs) with continued integration tests on the concrete `FilePathResolver` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T006`]. Tests verify:

- `ResolveFilePath` returns the strategy result.
- `TryResolveFilePath` returns `false` on exception (and `true` on success).
- The strategies are called in the documented order (PATH first, then directory recursion) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T006`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].
- The lowercasing contract in `GetPathFileExtensions` is verified end-to-end (the strategy loop uses the lowercased extensions as-is, with no per-iteration `.ToLower()`) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T006`].

The integration tests on the concrete continue to use the existing patterns (PATH-based resolution, file-existence checks) and do not require a rewrite [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T006`].

## Out of Scope (covered by PR1)

- Removal of `FilePathResolver.Shared` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D001`].
- Ctor fates for `ExternalProcessFactory()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T012`] and `ExternalProcess(ProcessConfiguration, ProcessExitConfiguration?)` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T013`].
- Benchmark file updates [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T004`].

## Verification

PR2's verification steps derive from T004, T005, T006, T010, T012, T013, T014, T015, D001, D003, D004, D006, D007, D009.

- `dotnet test` from `tests/CliInvoke.Tests/` passes.
- The 2 benchmark projects compile.
- `grep -r "GetPathInfo\|GetPathExtensionsInfo" src/ tests/` returns no results — the renames are complete.
- `grep -r "FilePathResolver.Shared" src/ benchmarks/ tests/` returns no results.
- The new `FilePathResolverBase` is referenced by `FilePathResolver` (extends) and by the `IFilePathResolver` consumers (via the base's `ResolveFilePath` and `TryResolveFilePath` methods).
- `CONTEXT.md` contains the design decisions section.
- The six `Run*` entry points acquire the resolver via `GetFilePathResolver()`; none of them throw on "not configured."

## Ledger Reference

- D001 — `FilePathResolver.Shared` fate (delete)
- D003 — `IFilePathResolver` failure semantics (gain `TryResolveFilePath`)
- D004 — `ResolverBase` / `Resolver` method distribution (abstract base, abstract strategies, virtual data accessors)
- D005 — `GetPathInfo` rename (superseded by D006)
- D006 — `GetPathInfo` rename (refined rationale: `Enumerate` for `IEnumerable`, `Get` for arrays)
- D007 — `FilePathResolverBase` package boundary (lives in `CliInvoke.Core`)
- D009 — `CliRun` resolver source (loud-failure rule dropped; `UseFilePathResolver` is optional)
- T001 — algorithm placement in `FilePathResolverBase` (wrapper catches `Exception`)
- T004 — migration scope (3 library sites + 2 benchmarks; PR1)
- T005 — `GetPathFileExtensions` casing strategy (lowercased in a single pass)
- T006 — test strategy (unit + integration)
- T007 — `Try*` method default implementation location (on the base)
- T008 — default lifetime of `IFilePathResolver` in `AddCliInvoke` (match the global lifetime parameter)
- T010 — refactor PR scope (two-PR split; PR2 covers the base class introduction)
- T012 — `ExternalProcessFactory()` parameterless ctor fate (PR1)
- T013 — `ExternalProcess(ProcessConfiguration, ProcessExitConfiguration?)` ctor fate (PR1)
- T014 — `UseFilePathResolver` optionality and lazy default (with lock)
- T015 — resolution order documentation location (in both source comment and `CONTEXT.md`)
