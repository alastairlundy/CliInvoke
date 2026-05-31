---
name: cliinvoke-pattern-validator
description: Validates that code changes adhere to the three primary CliInvoke invocation patterns (CliRun, IProcessInvoker, and IExternalProcess) to prevent architectural pattern bleed and maintain separation of concerns.
---

# CliInvoke Pattern Validator

Ensures that the implementation of process invocation adheres to the architectural boundaries defined in PATTERNS.md, preventing the mixing of high-level convenience APIs with low-level lifecycle management.

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
- **CliRun**: Must remain a thin wrapper. No complex state management or low-level process manipulation should be added directly to `CliRun`.
- **IProcessInvoker**: Must remain abstract and DI-friendly. No static dependencies or global state should be introduced into implementations. Ensure `ProcessConfiguration` is the primary input.
- **IExternalProcess**: Must maintain granular lifecycle control (Start -> optional Input -> Capture/Kill). Ensure the `IExternalProcessFactory` is used for instantiation rather than direct constructor calls where DI is expected.

### Step 3: Dependency Check
Verify that high-level patterns do not depend on low-level implementation details of other patterns (e.g., `CliRun` should not bypass `IProcessInvoker` to interact directly with `IExternalProcess`).

## Validation

- [ ] The change does not introduce "pattern bleed" (mixing responsibilities).
- [ ] No static dependencies were added to `IProcessInvoker` implementations.
- [ ] `CliRun` remains a zero-boilerplate entry point.
- [ ] The separation between Builders, Models, and Invokers is preserved.

## Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Adding complex logic to `CliRun` for convenience | Move logic into a new `IProcessInvoker` implementation or `ProcessConfiguration` extension. |
| Creating `IExternalProcess` via `new` in DI contexts | Use `IExternalProcessFactory` to maintain testability and abstraction. |
| Bypassing `IProcessInvoker` in `CliRun` | Ensure `CliRun` always delegates execution to the configured invoker. |
