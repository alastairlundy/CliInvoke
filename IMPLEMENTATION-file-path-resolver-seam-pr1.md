# Implementation Blueprint — File-Path Resolver Seam (PR1)

> **Scope**: PR1 of the file-path resolver seam refactor. PR1 removes `FilePathResolver.Shared` and updates the three library consumers + two benchmark files to use the new defaults. PR2 (separate blueprint) introduces `FilePathResolverBase` and the remaining structural changes.

## Scope Binding

- **Linked Spec**: Architecture-review candidate **C2** (make the file-path resolver seam real) — referenced in the Decision Ledger header.
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-file-path-resolver-seam.md`
- **Authorization notice**: This blueprint is a context pointer valid ONLY for the linked spec and the cited Decision Ledger. It must not be applied to other specifications without explicit authorization.

## Goals

PR1 accomplishes three things:

1. Remove the static `FilePathResolver.Shared` singleton from the public API [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D001`].
2. Migrate the three library sites that currently reference `Shared` to use `new FilePathResolver()` or an injected resolver [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T004`].
3. Update the two benchmark files that reference `Shared` to use `new FilePathResolver()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T004`].

The ctor fates — `ExternalProcessFactory()` keeps the parameterless ctor with a default of `new FilePathResolver()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T012`], and `ExternalProcess(ProcessConfiguration, ProcessExitConfiguration?)` keeps the ctor with the same default [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T013`] — are applied in this PR.

## Scope of Changes

### 1. `src/CliInvoke/Processes/ExternalProcess.cs` — line 48

The parameterless-resolver ctor currently does `_filePathResolver = FilePathResolver.Shared;`. Replace with `_filePathResolver = new FilePathResolver();` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T013`]. The ctor's XML comment must flag the implicit `FilePathResolver` allocation so users reading the source are not surprised [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T013` constraints].

### 2. `src/CliInvoke/Factories/ExternalProcessFactory.cs` — line 27

The parameterless ctor currently does `_filePathResolver = FilePathResolver.Shared;`. Replace with `_filePathResolver = new FilePathResolver();` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T012`]. The ctor's XML comment must flag the implicit allocation [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T012` constraints].

### 3. `src/CliInvoke/Extensions/CliRun.cs` — lines 22-23

The static factory lambda `_externalProcessFactory = () => new ExternalProcessFactory();` is unchanged at the source level. After PR1, the `ExternalProcessFactory` constructed by this lambda uses its parameterless ctor, which now defaults to `new FilePathResolver()` per T012. The `CliRun` resolver wiring (lazy default with lock per T014 / D009) is deferred to PR2.

### 4. `benchmarks/CliInvoke.Benchmarks/Data/BufferedTestHelper.cs`

Replace any `FilePathResolver.Shared` reference with `new FilePathResolver()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T004`].

### 5. `benchmarks/CliInvoke.Benchmarks/Data/DotnetCommandHelper.cs`

Replace any `FilePathResolver.Shared` reference with `new FilePathResolver()` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T004`].

## Out of Scope (deferred to PR2)

- Introduction of `FilePathResolverBase` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D007`].
- Method visibility change (`protected` → `protected abstract` on the strategies) [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D004`].
- Renames: `GetPathInfo` → `EnumeratePathDirectories`, `GetPathExtensionsInfo` → `GetPathFileExtensions` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D005`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D006`].
- Lowercasing in `GetPathFileExtensions` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T005`].
- `Try*` wrapper on the base [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T001`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T007`].
- `TryResolveFilePath` method on `IFilePathResolver` [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D003`].
- `CliRun.UseFilePathResolver` + lazy default + lock [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T014`], [`DECISIONS-CliInvoke-file-path-resolver-seam.md#D009`].
- Resolution order documentation [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T015`].

## Verification

PR1's verification steps derive from T004, T010, T012, T013, and D001.

- `dotnet test` from `tests/CliInvoke.Tests/` passes (CI workflow at `.github/workflows/test.yml` runs from this directory).
- The 2 benchmark projects compile.
- `grep -r "FilePathResolver.Shared" src/ benchmarks/` returns no results — the static singleton is fully removed.
- The 3 library sites compile and the XML comments document the implicit allocation.

## Mid-Migration State

After PR1 lands and before PR2 lands, the codebase is in a mid-migration state: `FilePathResolver.Shared` is removed, the concrete `FilePathResolver` is the only implementer of `IFilePathResolver`, methods are still `protected` (concrete) on the concrete, no base class yet [`DECISIONS-CliInvoke-file-path-resolver-seam.md#T010`]. This state is technically valid (compiles, tests pass) but architecturally mid-migration. PR2 must be merged after PR1 because PR2's base class introduction depends on PR1's `Shared` removal.

## Ledger Reference

- D001 — `FilePathResolver.Shared` fate (delete)
- T004 — migration scope (3 library sites + 2 benchmarks)
- T010 — refactor PR scope (two-PR split; PR1 covers `Shared` removal and ctor updates)
- T012 — `ExternalProcessFactory()` parameterless ctor fate (keep, default to `new FilePathResolver()`)
- T013 — `ExternalProcess(ProcessConfiguration, ProcessExitConfiguration?)` ctor fate (keep, default to `new FilePathResolver()`)
