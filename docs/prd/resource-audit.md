## Problem Statement

CliInvoke relies on several critical types that manage unmanaged OS resources and sensitive memory (e.g., `ProcessConfiguration`, `UserCredential`). Failure to dispose of these types leads to resource leaks or security vulnerabilities. Currently, detecting these leaks relies on manual review or non-deterministic LLM analysis, which is error-prone and inconsistent.

## Solution

Implement a deterministic resource audit system consisting of a Roslyn Diagnostic Analyzer and a corresponding development skill. The system will automatically flag instances of critical disposable types that are not properly managed via `using`/`await using` blocks or ownership transfer.

## User Stories

1. As a maintainer, I want the compiler to warn me when a `ProcessConfiguration` is instantiated without a `using` block, so that I don't leak OS handles.
2. As a maintainer, I want the compiler to warn me when `ProcessConfigurationBuilder` is not disposed, so that sensitive data in internal buffers is cleared.
3. As a maintainer, I want the compiler to warn me when `IExternalProcess` is not disposed, so that underlying native processes are cleaned up.
4. As a maintainer, I want the compiler to warn me when `UserCredential` or `UserCredentialBuilder` are not disposed, so that `SecureString` memory is cleared.
5. As a maintainer, I want the compiler to warn me when `PipedProcessResult` is not disposed, so that output streams are closed.
6. As a maintainer, I want the compiler to warn me when `ProcessWrapper` is not disposed, so that associated resources are released.
7. As an agent, I want a structured Markdown summary of all resource leaks in the codebase, so that I can fix them systematically without parsing noisy build logs.
8. As a developer, I want the analyzer to be isolated in its own project so that heavy Roslyn dependencies do not leak into the core library's dependency graph.
9. As a developer, I want the analyzer to be structured such that it can eventually be released as a public NuGet package to help library consumers.

## Implementation Decisions

- **Analyzer Residency**: Create a new project `CliInvoke.Analyzers` in a dedicated `analyzers/` top-level directory.
- **Dependency Management**: Use a separate `Directory.Packages.props` within the `analyzers/` directory to isolate Roslyn dependencies.
- **Analyzer Engine**: Implement as a Roslyn `DiagnosticAnalyzer`.
- **Detection Logic (v1)**: Employ a strict "Conservative" approach. Any instantiation of a critical disposable type must be wrapped in a `using`/`await using` block or passed to a method/constructor that takes ownership.
- **Critical Types List**:
    - `ProcessConfiguration`
    - `ProcessConfigurationBuilder`
    - `IExternalProcess`
    - `UserCredential`
    - `UserCredentialBuilder`
    - `PipedProcessResult`
    - `ProcessWrapper`
- **Skill Interface**: The `cliinvoke-resource-audit` skill will execute the build, filter diagnostics by the analyzer's ID (e.g., `CLI001`), and output a structured Markdown table.
- **Release Strategy**: Internal priority with a public-ready project structure.

## Testing Decisions

- **Analyzer Testing**: Use `Microsoft.CodeAnalysis.Testing` to write unit tests that verify specific C# code snippets trigger (or do not trigger) the `CLI001` diagnostic.
- **Behavioral Testing**: Tests must only verify the presence/absence of the diagnostic and the accuracy of the reported line/column, not the internal implementation of the analyzer.
- **Scope**: Only the `CliInvoke.Analyzers` module will be formally unit tested.

## Out of Scope

- Support for manual `.Dispose()` calls in complex lifecycles (deferred to v2).
- Public NuGet publication of the analyzer.
- Automatic code-fixing (CodeFixProvider) for the diagnostics.

## Further Notes

The "Ownership Transfer" logic in v1 will be a simple heuristic (e.g., the object is passed as an argument to another method) to reduce false positives while remaining deterministic.
