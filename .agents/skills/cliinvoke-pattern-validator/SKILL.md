---
name: cliinvoke-pattern-validator
description: Validates that code changes adhere to the three primary CliInvoke invocation patterns (CliRun, IProcessInvoker, and IExternalProcess) to prevent architectural pattern bleed and maintain separation of concerns. Use when updating or adding any code that touches upon process invocation.
---

# CliInvoke Pattern Validator

Ensures that process invocation code stays inside the architectural boundaries set out in `DESIGN_PATTERNS.md`, so high-level convenience APIs do not drift into low-level lifecycle management.

## When to Use

- When implementing new invocation logic or adding features to existing patterns.
- During code reviews of changes affecting `src/CliInvoke` or `src/CliInvoke.Core`.
- When refactoring process execution logic to move from one pattern to another.

## When Not to Use

- For general logic bugs that do not involve architectural patterns.
- For performance tuning that doesn't change the invocation pattern.
- When modifying non-invocation related code (e.g., logging utilities, DI registration helpers).

## Inputs

| Input | Required | Description |
|-------|----------|-------------|
| Target Files | Yes | The files or modules being modified or added. |
| Pattern Context | Yes | Which of the three patterns (`CliRun`, `IProcessInvoker`, `IExternalProcess`) is being targeted. |

## Workflow

### Step 1: Pattern Identification
Identify which pattern the change belongs to. If the change affects multiple patterns, treat them as separate validation units.

### Step 2: Boundary Validation
Depending on the pattern, verify the following constraints:
- **CliRun**: Must remain a thin wrapper, and it must remain stateless. It builds a fresh `ProcessInvocationPipeline` over a fresh `ExternalProcessFactory` on every call, so do not let it read or hold process-wide configuration, and do not add low-level process manipulation to it.
- **IProcessInvoker**: Must remain abstract and DI-friendly. No static dependencies or global state should be introduced into implementations. Ensure `ProcessConfiguration` is the primary input.
- **IExternalProcess**: Must maintain granular lifecycle control (Start -> optional Input -> Capture/Kill). Ensure the `IExternalProcessFactory` is used for instantiation rather than direct constructor calls where DI is expected.

### Step 3: Dependency Check
Verify that a high-level pattern is not reimplementing a lower-level pattern's lifecycle logic. Sharing plumbing is expected. `CliRun` legitimately constructs `ExternalProcessFactory` and `ProcessInvocationPipeline` itself rather than resolving an `IProcessInvoker`, because it has no container to resolve one from. What you are looking for is hand-rolled start, capture, or kill behavior that a lower-level type already owns.

## Validation

- [ ] The change does not introduce "pattern bleed" (mixing responsibilities).
- [ ] No static dependencies were added to `IProcessInvoker` implementations.
- [ ] `CliRun` remains a zero-boilerplate entry point, and holds no state between calls.
- [ ] Construction matches the v3 default, with `ProcessConfigurationBuilder` reserved for its documented use cases.
- [ ] The change does not introduce **v2-style code** (see `GLOSSARY.md`). Code is v2-style if (a) it uses APIs of the prior major version (v2) that v3 removed or changed, or (b) it defaults to v3-advanced construction styles — reaching for `ProcessConfigurationBuilder` by habit where init construction is the v3 default. **Carve-out**: deliberate advanced-builder usage (argument escaping, `UserCredentialSpec`/resource-policy callback flows) is legitimate and must not be flagged.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Adding complex logic to `CliRun` for convenience | Move logic into a new `IProcessInvoker` implementation or `ProcessConfiguration` extension. |
| Treating `CliRun`'s direct construction of `ExternalProcessFactory` and `ProcessInvocationPipeline` as pattern bleed | That is the intended design. `CliRun` is stateless and allocates a fresh pipeline per call (`DESIGN_PATTERNS.md`). Flag shared state and hand-rolled lifecycle logic, not fresh allocations. |
| Creating `IExternalProcess` via `new` in DI contexts | Use `IExternalProcessFactory` to maintain testability and abstraction. |
| Reading process-wide configuration inside `CliRun` | `CliRun` takes no configuration. Thread the value through the call, or move the caller to `IProcessInvoker`. |
| Emitting v2-style code (per `GLOSSARY.md` `v2-style code`) | Use init construction with `required` `TargetFilePath` as the default. Reach for `ProcessConfigurationBuilder` only when the use case requires argument escaping, `UserCredentialSpec`, or resource-policy callback flows. |
