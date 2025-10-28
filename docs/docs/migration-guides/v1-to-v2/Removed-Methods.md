# Removed Methods
These cover common use cases and are NOT an exhaustive list of method removals.

## IProcessInvoker and ProcessInvoker

### ExecuteProcessAsync without ProcessConfiguration

**v1 Method Signature**:
```csharp
Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessResultValidation processResultValidation, ProcessResourcePolicy? processResourcePolicy = null, CancellationToken cancellationToken = default);
```

**v2 Replacement**:
```csharp
Task<ProcessResult> ExecuteAsync(
ProcessConfiguration processConfiguration,
ProcessExitConfiguration? processExitConfiguration = null, bool disposeOfConfig = false,CancellationToken cancellationToken = default);
```

### ExecuteBufferedProcessAsync without ProcessConfiguration

**v1 Method Signature**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
ProcessResultValidation processResultValidation,
ProcessResourcePolicy? processResourcePolicy = null,CancellationToken cancellationToken = default);
```

**v2 Replacement**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedAsync(
    ProcessConfiguration processConfiguration,
    ProcessExitConfiguration? processExitConfiguration = null,
    bool disposeOfConfig = false,
    CancellationToken cancellationToken = default);
```
