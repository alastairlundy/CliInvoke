# CliInvoke Glossary

This document provides a glossary and architectural mapping of the CliInvoke domain. It is intended to ensure consistency across the codebase and provide unambiguous definitions for agents and developers.

## Core Concepts

### Resource-Owning Type
A type within the library that manages unmanaged OS resources (such as pipes, file handles, or process threads) or contains sensitive data in memory (such as `SecureString`). Because these resources are not managed by the .NET Garbage Collector, they require explicit lifecycle management.

### Process Invocation Pipeline
A layered interceptor pattern used to execute cross-cutting concerns (e.g., logging, path resolution, result validation) around the execution of an external process. The pipeline wraps the core process orchestration, allowing modifications to the configuration before execution and modifications to the result after execution.

### Process Invocation Context
The state-bearing object passed through the Process Invocation Pipeline. It encapsulates the requested configuration, the execution mode (Basic, Buffered, or Piped), and the resulting process output. It serves as the the single source of truth for middleware to communicate changes and state across the pipeline.


## Architectural Patterns

For detailed definitions, target audiences, and usage examples of the architectural patterns used in CliInvoke, refer to **[PATTERNS.md](PATTERNS.md)**.

## Design Decisions

### 1. Resolution order rationale

`FilePathResolverBase.ResolveFilePath` tries PATH first, then directory recursion. This order is a performance contract: PATH lookup is a fast environment-variable read and covers the common case; directory recursion is slow and rare. Reordering the two strategies requires a new decision record.

### 2. Get-prefix vs Enumerate-prefix convention

`GetPathFileExtensions` returns `string[]`; `EnumeratePathDirectories` returns `IEnumerable<string>?`. The asymmetry is intentional and performance-driven, not technical debt. The `Get`-prefix signals a materialised array (cheap repeated access, expensive re-enumeration); the `Enumerate`-prefix signals lazy enumeration (cheap to re-iterate, expensive to materialise). Do not "fix" the naming to match.

### 3. Lowercasing contract for `GetPathFileExtensions`

Custom resolvers overriding `GetPathFileExtensions` must return lowercased extensions. The base's default implementation lowercases in a single pass; the strategy loop uses the values as-is with no per-iteration `.ToLower()`. Returning raw (un-lowercased) extensions causes a silent runtime bug — symptom is "no match found" for a case that should match.

### 4. Catch discipline for `Try*` methods

`FilePathResolverBase.TryResolveFilePath` catches `Exception`, not `FileNotFoundException`. This follows the .NET `Try*` convention: the method must never propagate an exception. The broader catch is required by convention, not by the current algorithm (which only throws `FileNotFoundException` from `LocateFileFromDirectory`). Direct implementers of `IFilePathResolver` must follow the same discipline.

### 5. `IFilePathResolver` lifetime convention in `AddCliInvoke`

`AddCliInvoke` registers `IFilePathResolver` with the same lifetime as the global `lifetime` parameter (default `Scoped`). The resolver is not special-cased — a stateless service does not automatically become `Singleton`. Users who want a different lifetime opt in via `UseCustomFilePathResolver<TResolver>(ServiceLifetime)`.

### 6. `CliRun` defaults facade (no static state)

`CliRun` was previously a static facade backed by process-wide mutable state configured through `CliRun.UseExternalProcessFactory` / `CliRun.UseFilePathResolver`. Those `Use*` methods and their backing static fields/helpers were removed: every `Run*`/`FireAndForget` call now allocates a fresh `ProcessInvocationPipeline` (and a fresh `ExternalProcessFactory` with a default `FilePathResolver`) per call. There is therefore no shared lock or lazy-initialisation asymmetry to preserve — the historical `lock(_syncRoot)` double-check on the resolver no longer exists. Callers needing a custom factory or resolver must use `IProcessInvoker` (or DI) instead of `CliRun`.
*Cites: [`D002`](docs/decisions/DECISIONS-CliInvoke-design-smell-triage.md#D002), [`D013`](docs/decisions/DECISIONS-CliInvoke-design-smell-triage.md#D013), [`T014`](docs/decisions/DECISIONS-CliInvoke-file-path-resolver-seam.md#T014).*
