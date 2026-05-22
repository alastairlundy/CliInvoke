# Proposal: Middleware System for CliInvoke

## Preamble
Following the separation of result validation logic from `ProcessInvoker` in v3, CliInvoke will introduce an optional middleware system for `ProcessInvoker` and potentially `ExternalProcess`. 

The primary goals of this system are:
- **Decoupling**: Move cross-cutting concerns (logging, diagnostics, resilience) out of the core invoker.
- **Dependency Reduction**: De-couple file path resolution from `ProcessInvoker` implementations, allowing these to be shifted into `CliInvoke.Extensions` or separate implementation packages.
- **Extensibility**: Provide a standardized way for users to manipulate process configurations before execution and results after execution.

---

## Architectural Design

### 1. The Middleware Pipeline
CliInvoke will implement a **layered middleware approach**. Execution will be wrapped in a pipeline where each middleware can perform actions both before and after the underlying process is executed.

### 2. ProcessInvocationContext
To maintain state across the pipeline, a `ProcessInvocationContext` object will be passed through the chain. 

**Context Properties:**
- **`ProcessConfiguration Configuration`**: The configuration to be used for the process (modifiable by pre-processing middleware).
- **`ProcessExitConfiguration? ExitConfiguration`**: The exit behavior configuration.
- **`CancellationToken CancellationToken`**: The active cancellation token.
- **`InvocationMode Mode`**: An enum (`Basic`, `Buffered`, `Piped`) indicating which `Execute` method was triggered, allowing middleware to adjust behavior before the result exists.
- **`ProcessResult? Result`**: The resulting process data, set after the "leaf" execution.

### 3. Type Handling and Polymorphism
To avoid introducing complex new interfaces, the system will utilize the existing `ProcessResult` class hierarchy:
- The context will store the result as the base `ProcessResult` type.
- Middleware requiring specialized data (e.g., manipulating output strings in a `BufferedProcessResult` or streams in a `PipedProcessResult`) will use **explicit casting** to the derived type.

---

## Implementation Strategy

### 1. Integration in `ProcessInvoker`
Middleware support will be integrated directly into the `ProcessInvoker` class while remaining completely optional for non-middleware users.

- **Constructor Overloading**: 
    - Existing users continue using `ProcessInvoker(IFilePathResolver filePathResolver)`.
    - Middleware users use `ProcessInvoker(IFilePathResolver filePathResolver, IEnumerable<IProcessMiddleware>? middlewares = null)`.
- **Private Method Delegation**: 
    - The public `Execute...` methods will initialize the `ProcessInvocationContext` and trigger the middleware chain.
    - The final step (the "leaf") of the pipeline will delegate to a private `ExecuteCoreAsync` method containing the actual OS process orchestration logic.

### 2. Distribution and Packaging
- **Core Logic**: The middleware interfaces and `ProcessInvocationContext` will reside in the core package to ensure fundamental compatibility.
- **Extensions**: Common middleware implementations (e.g., Default Path Resolver, Logging, Validation) and DI registration helpers will be placed in `CliInvoke.Extensions`.
- **Specializations**: Complex or platform-specific middleware may be moved to separate NuGet packages to keep the main payload lean.

---

## Supported Use Cases
The design specifically enables the following scenarios:
- **Path Resolution**: Decoupled from the core invoker; implemented as pre-processing middleware.
- **Logging/Diagnostics**: Tracking execution duration, resource usage, and start/stop timestamps.
- **Result Manipulation**: Modifying `StandardOutput` or `StandardError` before they are returned to the user.
- **Resilience**: Implementing retry logic or fault tolerance around the `ExecuteCoreAsync` call.
- **Validation**: Executing validation rules against the `ProcessResult` post-execution.