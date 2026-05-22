# CliInvoke Context

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
