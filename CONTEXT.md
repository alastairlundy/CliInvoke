# CliInvoke Context

This document provides a glossary and architectural mapping of the CliInvoke domain. It is intended to ensure consistency across the codebase and provide unambiguous definitions for agents and developers.

## Core Concepts

### Resource-Owning Type
A type within the library that manages unmanaged OS resources (such as pipes, file handles, or process threads) or contains sensitive data in memory (such as `SecureString`). Because these resources are not managed by the .NET Garbage Collector, they require explicit lifecycle management.

### ProcessConfiguration
A value object that encapsulates the settings required to start an external process, including the target file path, command-line arguments, working directory, and output redirection mode. It is designed to be immutable and disposable when it contains resource-owning references.

### ProcessExitConfiguration
A value object that defines how a process should be terminated and what conditions constitute successful completion. It includes timeout policies, interrupt strategies, and success criteria.

### ProcessResult
A base class representing the outcome of a process execution, containing at minimum the exit code. Specialized variants include:
- `BufferedProcessResult`: Adds captured standard output and standard error streams
- `PipedProcessResult`: Adds capabilities for inter-process communication through pipes

### IExternalProcessFactory
An abstraction responsible for creating `IExternalProcess` instances with specific configurations. This factory pattern allows for customization of process creation logic while maintaining separation of concerns.

### ExternalProcess
The default implementation of `IExternalProcess` that wraps a system process and provides asynchronous methods for starting, waiting, and capturing process output. It implements proper resource disposal through the `IAsyncDisposable` pattern.

### Process Invocation Pipeline
A layered interceptor pattern used to execute cross-cutting concerns (e.g., logging, path resolution, result validation) around the execution of an external process. The pipeline wraps the core process orchestration, allowing modifications to the configuration before execution and modifications to the result after execution.

### Process Invocation Context
The state-bearing object passed through the Process Invocation Pipeline. It encapsulates the requested configuration, the execution mode (Basic, Buffered, or Piped), and the resulting process output. It serves as the the single source of truth for middleware to communicate changes and state across the pipeline.


## Architectural Patterns

For detailed definitions, target audiences, and usage examples of the architectural patterns used in CliInvoke, refer to **[PATTERNS.md](PATTERNS.md)**.
