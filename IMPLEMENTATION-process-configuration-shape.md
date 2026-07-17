# Implementation Blueprint — ProcessConfiguration Shape

> **Context pointer**: This blueprint is valid ONLY for the linked Decision Ledger. Do not apply it to other specifications without explicit authorization.

## Scope Binding

- **Linked Spec**: [`docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md`](docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md)
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-process-configuration-shape.md`

## Summary

The Decision Ledger records 14 functional decisions (D001–D014) about the shape of `ProcessConfiguration` in CliInvoke. This blueprint synthesizes the four technical decisions (T001–T004) into actionable refactor steps. The refactors collapse the two ctors into a single canonical shape, rename the wrapper subclass to `BuilderProcessConfiguration`, remove dead setters, add a documentation contract on the renamed wrapper, and enforce broken-cref build failure.

## Ledger Reference

Every record in the Decision Ledger that this blueprint cites:

- `D001` — `ProcessConfiguration` shape (frozen at construction except `TargetFilePath`)
- `D004` — `ProcessConfiguration` shape re-opened (wrapper stays, rename pending). *Supersedes D001.*
- `D005` — 15-param ctor accessibility (`protected`). *Supersedes D002.*
- `D007` — Wrapper rename to `BuilderProcessConfiguration`
- `D008` — Remarks block format (`<remarks>` only, no `<summary>` extension)
- `D009` — Remarks block scope (ctor-availability, no-delete rule, long-term mention)
- `D010` — Remarks block cross-references (`<see cref="ProcessConfigurationBuilder" />`)
- `D011` — 3-param ctor delegation to 15-param ctor
- `D012` — Setter removal on `Arguments` and `OutputRedirection`
- `D013` — `TargetFilePath` setter visibility (`public`)
- `T001` — `arguments` null check moved to 15-param ctor
- `T002` — `BuilderProcessConfiguration`'s `outputRedirection` default stays `false` (intentional)
- `T003` — Targeted cref warnings as errors in `Directory.Build.props`
- `T004` — Remarks block structure (three `<para>` elements)

## Implementation Steps

### Step 1 — Move `arguments` null check to 15-param ctor

Satisfies [`DECISIONS-CliInvoke-process-configuration-shape.md#T001`], [`...#D011`], [`...#D005`], [`...#D001`], [`...#D004`].

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`:

1. Add `ArgumentNullException.ThrowIfNull(arguments);` to the 15-param ctor (line 76), placed immediately after the existing `ArgumentException.ThrowIfNullOrEmpty(targetFilePath);` check.
2. Refactor the 3-param ctor body (lines 32–57) to delegate to the 15-param ctor via `: this(targetFilePath, arguments, redirectStandardInput: false, outputRedirection: outputRedirection)` per [`DECISIONS-CliInvoke-process-configuration-shape.md#D011`]. The 3-param ctor body becomes empty.
3. The `ArgumentNullException.ThrowIfNull(arguments);` at line 33 is removed (moved to the 15-param ctor).
4. The line 49 dead-code bug (`RedirectStandardInput = StandardInput != StreamWriter.Null;`, which is always `false` because `StandardInput` is set to `StreamWriter.Null` on line 47) becomes moot and is removed.

**Acceptance criteria**:

- `new ProcessConfiguration("foo.exe", null)` throws `ArgumentNullException` with paramName `arguments` (preserved from current public API per [`DECISIONS-CliInvoke-process-configuration-shape.md#T001`]).
- `new ProcessConfiguration("foo.exe")` (default `arguments = ""`) does not throw.
- `new ProcessConfiguration("foo.exe", "arg1")` does not throw.
- `new ProcessConfiguration(null, "arg1")` throws `ArgumentException` via `ThrowIfNullOrEmpty` on `targetFilePath`.

### Step 2 — Remove dead setters on `Arguments` and `OutputRedirection`

Satisfies [`DECISIONS-CliInvoke-process-configuration-shape.md#D012`], [`...#D001`], [`...#D004`].

In `src/CliInvoke.Core/Primitives/ProcessConfiguration.cs`:

1. Line 121: change `public string Arguments { get; protected set; }` to `public string Arguments { get; }`.
2. Line 168: change `public bool OutputRedirection { get; protected set; }` to `public bool OutputRedirection { get; }`.
3. The 15-param ctor continues to set the backing fields directly via direct field assignment (which is permitted for `{ get; }` auto-properties).

**Acceptance criteria**:

- No production, test, or benchmark code calls `.Arguments =` or `.OutputRedirection =` (verified by grep before the change per [`DECISIONS-CliInvoke-process-configuration-shape.md#D012`]).
- The build succeeds after the change.
- The frozen-type contract from [`DECISIONS-CliInvoke-process-configuration-shape.md#D001`]/[`...#D004`] is fully honoured: `TargetFilePath` is the only mutable property.

### Step 3 — Rename wrapper to `BuilderProcessConfiguration`

Satisfies [`DECISIONS-CliInvoke-process-configuration-shape.md#D007`], [`...#T002`], [`...#D005`], [`...#D013`].

In `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`:

1. Line 426: rename `internal class ProcessConfigurationWrapper : ProcessConfiguration` to `internal class BuilderProcessConfiguration : ProcessConfiguration`.
2. Line 428: the ctor's `outputRedirection = false` default is preserved per [`DECISIONS-CliInvoke-process-configuration-shape.md#T002`] (the builder-centric default stays `false` to benefit users of the builder; this divergence from the 15-param ctor's `true` default is intentional).
3. Line 408: update the call site to use the new name: `BuilderProcessConfiguration configuration = new(_targetFilePath, arguments, ...);`.
4. The class remains `internal` per [`DECISIONS-CliInvoke-process-configuration-shape.md#D005`] (the wrapper's access modifier is unchanged).

**Acceptance criteria**:

- The class is renamed throughout the file.
- The call site at line 408 is updated.
- No other code references the old name `ProcessConfigurationWrapper` (verified by grep).
- The `outputRedirection = false` default is preserved per [`DECISIONS-CliInvoke-process-configuration-shape.md#T002`].
- The `TargetFilePath` setter visibility remains `public` per [`DECISIONS-CliInvoke-process-configuration-shape.md#D013`] (the rename does not touch the setter).

### Step 4 — Add remarks block to `BuilderProcessConfiguration`

Satisfies [`DECISIONS-CliInvoke-process-configuration-shape.md#D008`], [`...#D009`], [`...#D010`], [`...#T004`].

In `src/CliInvoke/Builders/ProcessConfigurationBuilder.cs`, add the following XML doc to `BuilderProcessConfiguration`:

```xml
/// <summary>
///     An internal subclass of <see cref="ProcessConfiguration" /> used by
///     <see cref="ProcessConfigurationBuilder" /> to invoke the protected
///     multi-parameter constructor.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ProcessConfigurationBuilder" /> lives in a different
///         assembly (<c>CliInvoke</c>) than <see cref="ProcessConfiguration" />
///         (<c>CliInvoke.Core</c>), and the multi-parameter constructor on
///         <see cref="ProcessConfiguration" /> is <c>protected</c>. This wrapper
///         is the legitimate cross-assembly access path that lets the builder
///         invoke the protected constructor without exposing it publicly.
///     </para>
///     <para>
///         Do not delete this class without first choosing a long-term
///         replacement.
///     </para>
///     <para>
///         A long-term solution to eliminate this wrapper is being developed.
///         See <see cref="ProcessConfigurationBuilder" /> for the only consumer.
///     </para>
/// </remarks>
internal class BuilderProcessConfiguration : ProcessConfiguration
{
    ...
}
```

**Acceptance criteria**:

- The remarks block contains three `<para>` elements per [`DECISIONS-CliInvoke-process-configuration-shape.md#T004`], one per [`DECISIONS-CliInvoke-process-configuration-shape.md#D009`] element.
- The `<see cref="ProcessConfigurationBuilder" />` cross-reference is in the third `<para>` per [`DECISIONS-CliInvoke-process-configuration-shape.md#D010`].
- The `<summary>` is minimal per [`DECISIONS-CliInvoke-process-configuration-shape.md#D008`].
- A broken cref (e.g., `<see cref="NonExistentClass" />`) fails the build per [`DECISIONS-CliInvoke-process-configuration-shape.md#T003`].
- The remarks block does not reference any `Dxxx` ID or ledger filename per [`DECISIONS-CliInvoke-process-configuration-shape.md#D009`].

### Step 5 — Add targeted cref warnings as errors to `Directory.Build.props`

Satisfies [`DECISIONS-CliInvoke-process-configuration-shape.md#T003`], [`...#D010`], [`...#D008`], [`...#D009`].

In `Directory.Build.props`, add a new property to the existing `<PropertyGroup>`:

```xml
<WarningsAsErrors>CS1574;CS1580;CS1581;CS1584;CS1658;CS1734;CS1762</WarningsAsErrors>
```

**Acceptance criteria**:

- The list of cref warning codes matches the Decision Ledger: CS1574, CS1580, CS1581, CS1584, CS1658, CS1734, CS1762.
- Existing `#pragma warning disable` blocks are unaffected: CS0618 at `ProcessConfiguration.cs:91–93`, CS8602 at `ProcessConfiguration.cs:217, 233`, CA1416 at `ProcessConfigurationBuilder.cs:18, 445`.
- A broken cref in the wrapper's remark fails the build (verified by introducing a broken cref, confirming the build fails, then reverting).
- The build succeeds with all valid crefs.

## Implementation Order

The steps have the following dependencies:

1. **Step 1** [`DECISIONS-CliInvoke-process-configuration-shape.md#T001`] — foundation; the validation must live in the 15-param ctor before the 3-param ctor's body becomes empty.
2. **Step 2** [`DECISIONS-CliInvoke-process-configuration-shape.md#D012`] — independent of Steps 1 and 3; can be done in parallel.
3. **Step 3** [`DECISIONS-CliInvoke-process-configuration-shape.md#D007`] — independent of Steps 1 and 2; can be done in parallel.
4. **Step 4** [`DECISIONS-CliInvoke-process-configuration-shape.md#D008`]/[`...#T004`] — depends on Step 3 (the wrapper must be renamed first).
5. **Step 5** [`DECISIONS-CliInvoke-process-configuration-shape.md#T003`] — independent; can be done at any time.

Recommended order: **Step 1 → Step 2 → Step 3 → Step 4 → Step 5**. Step 5 can be moved earlier if desired.

## Test Strategy

Run `dotnet test` from `tests/CliInvoke.Tests/` after each step (per `AGENTS.md` working-directory convention).

- **Step 1**: Add a test that `new ProcessConfiguration("foo.exe", null)` throws `ArgumentNullException` with paramName `arguments`. Verify the existing tests pass.
- **Step 2**: Verify the existing tests pass; no new tests needed (the setters are dead per [`DECISIONS-CliInvoke-process-configuration-shape.md#D012`]).
- **Step 3**: Verify the existing tests pass; no new tests needed (rename only).
- **Step 4**: Verify the existing tests pass; no new tests needed (documentation only).
- **Step 5**: Verify the build succeeds; manually introduce a broken cref to confirm the build fails, then revert.

## Deferred Work

The following decisions are deferred and are NOT part of this blueprint:

- **`D006`** — long-term solution for eliminating the wrapper (pivot to `Process Invocation Context`, public static `Create` factory, or parameter-object pattern). Deferred at user's request.
- **`D014`** — `ProcessConfigurationFactory` overload duplication. Deferred to another session.

These will be addressed in separate sessions.
