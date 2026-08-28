# Implementation Blueprint — CliInvoke v3 Internals Visibility

## Scope Binding

- **Linked Spec**: `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md`
- **Decision Ledger**: `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md`
- **Notice**: This blueprint is a context pointer valid ONLY for the linked Decision Ledger and must not be applied to other specifications without explicit authorization.

## Version window

`T002` removes the public `FilePathResolverBase` (a breaking change) and must ship in the v3 major version (`D006`). All other changes are non-breaking: internal relocations and removal of unused `InternalsVisibleTo` grants.

## File changes

### `src/CliInvoke.Core/CliInvoke.Core.csproj`
- Remove `<InternalsVisibleTo>` for `CliInvoke.Specializations` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009` (grant becomes unused after `T003`).
- Remove `<InternalsVisibleTo>` for `CliInvoke.Specializations.Tests` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009` (grant becomes unused after `T003`).

### `src/CliInvoke/CliInvoke.csproj`
- Remove `<InternalsVisibleTo>` for `CliInvoke.Extensions` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009`.
- Remove the `AssemblyAttribute`-based `InternalsVisibleTo` for `CliInvoke.Tests` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009` (redundant with `AssemblyInfo.cs`; both declarations removed).

### `src/CliInvoke/AssemblyInfo.cs`
- Remove `[InternalsVisibleTo("CliInvoke.Specializations")]` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009`.
- Remove `[InternalsVisibleTo("CliInvoke.Specializations.Tests")]` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009`.
- Remove `[InternalsVisibleTo("CliInvoke.Tests")]` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009`.

### `src/CliInvoke.Specializations/CliInvoke.Specializations.csproj`
- Remove `<InternalsVisibleTo>` for `CliInvoke.Specializations.Tests` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D009`.

### `src/CliInvoke.Core/FilePathResolverBase.cs`
- Delete the public abstract class `FilePathResolverBase` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T002` (v3 breaking; `D006`).

### `src/CliInvoke.Core/Internal/IO/PathEnvironmentVariable.cs`
- Delete (logic moves to CliInvoke) — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T002`.

### `src/CliInvoke/Internal/IO/PathEnvironmentVariable.cs` (new file)
- Add `internal static class PathEnvironmentVariable` (PATH/PATHEXT enumeration) in CliInvoke — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T002`.

### `src/CliInvoke/FilePathResolver.cs`
- Rework to resolve paths without the removed Core base class or Core's internal escaper; implement `IFilePathResolver` directly and use the new CliInvoke `PathEnvironmentVariable` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T002`.

### `tests/CliInvoke.Tests/Resolvers/FilePathResolverBaseTests.cs`
- Rework the `TestableFilePathResolver : FilePathResolverBase` stub (base class removed) — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T002`.

### `src/CliInvoke.Core/Internal/ShellArgumentEscaper.cs`
- Delete (logic moves to Specializations; `ArgumentsSpec` no longer references it) — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.

### `src/CliInvoke.Core/Configuration/ArgumentsSpec.cs`
- Remove `using CliInvoke.Core.Internal;` if it was only for `ShellArgumentEscaper` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.
- `EscapeCharacters`: `var e = ArgumentEscaper.EscapeInner(v); return ArgumentEscaper.NeedsQuoting(v) ? $"\"{e}\"" : e;` — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T007`.
- `EscapeCharactersWithoutWrapping`: call `ArgumentEscaper.EscapeInner(v)` per value before the existing group-wrap — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T007`.
- Note: existing Windows builder tests (drop newlines, double `"`, pass through `\`/`\t`) must remain green on Windows — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T006`.

### `src/CliInvoke.Core/Internal/ArgumentEscaper.cs` (new file)
- Add `internal static class ArgumentEscaper` with `public static string EscapeInner(string? argument)` (platform-aware inner escaping: Windows doubles `"`, leaves shell metacharacters unescaped, drops newlines; Unix escapes `'`) and `public static bool NeedsQuoting(string? argument)` (true when the argument requires double-quote wrapping on the current OS) — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T006`, `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T007`.

### `src/CliInvoke.Specializations/Middleware/PowerShellMiddleware.cs`
- Replace `ShellArgumentEscaper.EscapeForPowerShell(...)` with the new Specializations-internal escaper — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.

### `src/CliInvoke.Specializations/Middleware/CmdMiddleware.cs`
- Replace `ShellArgumentEscaper.EscapeForCmd(...)` with the new Specializations-internal escaper — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.

### `src/CliInvoke.Specializations/Internal/ShellArgumentEscaper.cs` (new file)
- Add `internal static class ShellArgumentEscaper` with `EscapeForPowerShell` and `EscapeForCmd` logic moved from Core — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.

### `tests/CliInvoke.Specializations.Tests/Internal/ShellArgumentEscaperTests.cs`
- Update references to test the new Specializations-internal escaper — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#T003`.

### `docs/adr/XXXX-ivt-minimization.md` (new ADR; assign next sequential number)
- Record the `D002` principle (minimize + promote/relocate) and the `D003`–`D006` decisions — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D007`.

### `CONTRIBUTING.md`
- Document the IVT-minimization principle: a new grant requires justification; unused grants are removed; test grants are excluded from reduction — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D007`.

### `GLOSSARY.md`
- Verify the four terms from `D008` are present (InternalsVisibleTo grant, Cross-package coupling point, Polyfill leakage, Entrypoint package); add if missing — `docs/decisions/DECISIONS-CliInvoke-v3-internals-visibility.md#D008`.

## Ledger Reference

- **Decisions**: D001, D002, D003, D004, D005, D006, D007, D008, D009
- **Info**: I001, I002
- **Technical**: T001, T002, T003, T004, T005, T006, T007

## Remaining grants after this pass

- **Kept (used):** `CliInvoke.Core → CliInvoke` (MiddlewareChain, `T001`); `CliInvoke.Specializations → CliInvoke.Extensions` (PowerShellMiddleware/CmdMiddleware, `T004`/`T005`); `CliInvoke.Core → CliInvoke.Tests`; `CliInvoke.Extensions → CliInvoke.Tests`.
- **Removed:** the five unused grants (`D009`) plus the two `CliInvoke.Core → CliInvoke.Specializations` grants that became unused after `T003`.
