# PRD: Middleware System for CliInvoke

## Problem Statement

Currently, cross-cutting concerns such as logging, diagnostics, resilience, and result validation are tightly coupled within the `ProcessInvoker` or handled manually by the user. This makes the core invoker bloated, creates unnecessary dependencies on file path resolution logic, and limits the ability for users to extend the process execution lifecycle without modifying the library source code.

## Solution

Introduce an optional, layered middleware system for process invocation. This system allows developers to intercept the execution request before the process starts and manipulate the result after the process exits. By decoupling these concerns into a pipeline, the library becomes more extensible, and the core primitive (`ExternalProcess`) remains focused on OS-level orchestration.

## User Stories

1. As a developer, I want to track the total execution time of a process, so that I can identify performance bottlenecks.
2. As a developer, I want to implement a retry policy that automatically restarts a process if it exits with a specific error code, so that my application is more resilient.
3. As a developer, I want to automatically resolve relative file paths to absolute paths before execution using a custom resolution strategy, so that I can support various configuration sources.
4. As a developer, I want to log every process start and exit event to a centralized logging system, so that I have an audit trail of execution.
5. As a developer, I want to sanitize or modify the `StandardOutput` of a process before it is returned to the application, so that I can remove noise or format the output.
6. As a developer, I want to validate the `ProcessResult` against a set of rules immediately after execution, so that I can throw meaningful domain exceptions instead of generic exit code errors.
7. As a developer, I want to use a standalone `ExternalProcess` without the `ProcessInvoker` but still benefit from the middleware pipeline, so that I can have fine-grained control over the process lifecycle.
8. As a developer, I want to configure the middleware pipeline using a fluent API, so that I can explicitly control the order of execution.
9. As a developer, I want to register my middleware via Dependency Injection, so that it integrates seamlessly with my existing .NET application infrastructure.
10. As a developer, I want the library to remain lightweight and not force DI dependencies on me if I am not using them, so that I can keep my lauch-time and dependency graph minimal.

## Implementation Decisions

### Architectural Framework
- **Invocation Level**: The middleware wraps the *act of invoking* the process, not the instance of the process object.
- **Chain of Responsibility**: The system uses a single-method pipeline pattern: `Task<ProcessResult> InvokeAsync(ProcessInvocationContext context, Func<ProcessInvocationContext, Task<ProcessResult>> next)`.
- **Return Value Pattern**: Results are passed back up the chain as return values rather than solely relying on side-effects within the context.

### Core Components
- **`ProcessInvocationContext`**: A state container holding the `IExternalProcess` instance, `ProcessConfiguration`, `InvocationMode` (Basic, Buffered, Piped), and the `CancellationToken`.
- **`ProcessInvocationPipeline`**: The engine that composes the middleware chain and executes the "leaf" (the actual process run).
- **`PipelineBuilder`**: A fluent API for manual pipeline construction to ensure a non-DI path exists.
- **`ExternalProcess` Primitive**: Modified to perform basic file path resolution *only if* the `TargetFilePath` is not already absolute. This allows middleware to override resolution.

### Packaging and Distribution
- **Core**: `IProcessMiddleware`, `ProcessInvocationContext`, and the `ProcessInvocationPipeline` reside in the core project.
- **Implementations**: Fundamental middleware (Path Resolution, Logging, etc.) will reside in the main `CliInvoke` package to avoid forcing transitive DI dependencies.
- **Extensions**: DI registration helpers and high-level integration logic will reside in `CliInvoke.Extensions`.

## Testing Decisions

- **Behavior-Driven Testing**: Tests will verify the outcomes of the pipeline (e.g., "Was the path resolved?", "Was the result modified?") rather than the internal order of delegate calls.
- **Isolation**: Middleware will be tested in isolation by providing a dummy `next` delegate.
- **Pipeline Orchestration**: The `ProcessInvocationPipeline` will be tested to ensure it correctly handles the transition from pre-processing to leaf execution and back through post-processing.
- **Regression**: Integration tests will ensure `ProcessInvoker` maintains its existing public API behavior while using the new pipeline internally.

## Out of Scope

- Creating a global "default" pipeline that cannot be overridden.
- Middleware that modifies the `ExternalProcess` internal `ProcessWrapper` state directly.
- Support for asynchronous streaming middleware that modifies data *during* the process run (pipeline only handles pre- and post-execution).

## Further Notes

- This PRD was drafted by Kilo.
- The "leaf" of the pipeline is the orchestration of `ExternalProcess.StartAsync` and the corresponding `Capture...` method based on the `InvocationMode`.
