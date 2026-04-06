# CliInvoke API Feedback: Improving Usability and Adoption

This document provides brutally honest, constructive feedback on CliInvoke's API surface based on a comprehensive analysis of the codebase. The goal is to identify friction points and suggest improvements to make the library easier to use and adopt.

---

## 1. Core Architecture Issues


### 1.3 Redirect Configuration is Conceptually Confused

**Problem:**  
Three boolean flags (`RedirectStandardInput`, `RedirectStandardOutput`, `RedirectStandardError`) don't fully define behavior. The actual stream handling depends on:
- Whether you call `ExecuteBufferedAsync()` (captures) vs `ExecuteAsync()` (streams to console)
- Separate properties like `StandardInput` stream
- Two separate concerns (redirect yes/no + what to do with stream) are tangled

**Impact:**  
Misconfiguration is common: set `RedirectStandardOutput = true` but forget to use `ExecuteBufferedAsync()`, get nothing captured.

**Recommendation:**  
Create a `StreamRedirectionMode` enum:
```csharp
public enum StreamRedirectionMode
{
    None,          // Don't redirect
    Buffer,        // Redirect and buffer (ExecuteBufferedAsync)
    Pipe,          // Redirect and pipe (ExecutePipedAsync)
    Console        // Redirect to parent console (ExecuteAsync)
}
```
Unify the concept: single configuration, execution method inferred or validated.

---

## 2. Naming and Discoverability

### 2.3 ProcessExitConfiguration Presets Have Cryptic Names (HIGH PRIORITY)

**Problem:**  
```csharp
ProcessExitConfiguration.Default
ProcessExitConfiguration.DefaultNoException
ProcessExitConfiguration.NoTimeoutDefault
ProcessExitConfiguration.NoTimeoutNoException
```

What does "Default" actually mean?
- Timeout: 30 minutes?
- Exit code validation: required zero?
- Behavior on timeout: throw or suppress?

Users don't know. They pick wrong.

**Impact:**  
Incorrect behavior in production. Unexpected exceptions or silent failures.

**Recommendation:**  
Replace with self-documenting presets:
```csharp
ProcessExitConfiguration.RequireZeroExitCodeWith30MinTimeout
ProcessExitConfiguration.AllowAnyExitCodeWith30MinTimeout
ProcessExitConfiguration.NoTimeoutRequireZeroExitCode
ProcessExitConfiguration.NoTimeoutAllowAnyExitCode
```

Or even better, use a fluent builder:
```csharp
new ProcessExitConfiguration()
    .RequireExitCodeZero()
    .WithTimeout(TimeSpan.FromMinutes(30))
    .Build()
```

---

## 3. Adoption Friction

### 3.1 DI Integration Requires Understanding Many Services

**Problem:**  
`AddCliInvoke()` registers 10+ services:
- `IProcessInvoker`, `IProcessConfigurationFactory`
- `IProcessResultValidator<T>` (for multiple result types)
- `IExecutableFileDetector`, `IPathEnvironmentVariableResolver` (external, from WhatExec.Lib)

If registering manually, it's easy to forget one service. The error messages don't guide you to the missing dependency.

**Impact:**  
Friction for advanced users who customize DI. Opaque errors when something is missing.

**Recommendation:**  
1. Create `AddCliInvokeWithDefaults()` that validates all dependencies are registered
2. Provide a single extension that covers everything with clear error messages if incompletely configured
3. Document the dependency graph explicitly

---

### 3.2 ProcessConfiguration Disposal is Easy to Miss

**Problem:**  
`ProcessConfiguration` is `IDisposable` (disposes `Credential` and `StandardInput`), but examples rarely dispose:
```csharp
ProcessConfiguration config = factory.Create("cmd.exe");
await invoker.ExecuteAsync(config);
// Leaked: credential, StandardInput not disposed
```

The `disposeOfConfig` parameter in `ExecuteAsync` is easy to overlook.

**Impact:**  
Resource leaks. Credentials left in memory. Streams not closed.

**Recommendation:**  
Consider one of:
1. **Make ProcessConfiguration NOT disposable.** Use a factory pattern to manage lifetime internally.
2. **Enforce `using` declarations.** Change the factory return type to a disposable wrapper:
   ```csharp
   using var config = factory.Create("cmd.exe");
   ```
3. **Auto-dispose at execution time.** The execution methods handle disposal automatically.

Document the choice clearly.

---

### 3.3 Cancellation and Exception Handling Are Entangled

**Problem:**  
`ProcessCancellationPolicy` combines two orthogonal concerns:
- **CancellationMode**: Forceful, Graceful, or None
- **ExceptionBehavior**: AllowExceptions or Suppress

Users must set both to express one intent: "gracefully cancel and suppress exceptions." This is over-configuration.

**Impact:**  
API feels complex. Users don't understand the interaction.

**Recommendation:**  
Simplify to a single `CancellationStrategy`:
```csharp
public enum CancellationStrategy
{
    Graceful,              // Try Interrupt, wait, then Kill
    Forceful,              // Immediate Kill
    GracefulWithTimeout,   // Graceful, but with timeout
    SuppressExceptions     // Graceful, suppress timeout exceptions
}
```

Or restructure:
```csharp
ProcessCancellationPolicy.Graceful()
    .WithTimeoutBehavior(TimeoutBehavior.ThrowException)
```

---

## 4. Feature Sprawl and Visibility

### 4.1 ProcessResourcePolicy is Undiscoverable and Platform-Limited

**Problem:**  
`ProcessResourcePolicy` is powerful but:
1. No examples in README
2. Platform-specific attributes are scattered:
   ```csharp
   [UnsupportedOSPlatform("ios")]
   public nint? MinWorkingSet { get; }
   ```
   Class-level platform checks would be clearer.
3. Feels like a hidden feature

**Impact:**  
Feature is rarely used. Windows-only resource limits are not obvious on other platforms.

**Recommendation:**  
1. Add examples to README: "Setting CPU affinity, memory limits, priority"
2. Mark the entire class with platform attributes or provide a platform check method
3. Create factory methods:
   ```csharp
   ProcessResourcePolicy.SetProcessorAffinity(nint affinity)
   ProcessResourcePolicy.SetMemoryLimits(nint min, nint max)
   ProcessResourcePolicy.SetPriority(ProcessPriorityClass priority)
   ```

---

### 4.2 Invalid Configurations Only Fail at Runtime

**Problem:**  
Shell execution + standard input redirection combination throws `ArgumentException` at runtime:
```csharp
builder
    .ConfigureShellExecution(true)
    .SetStandardInputPipe(stream)  // Compiles fine
    .Build()
    .ExecuteAsync()  // Runtime error: "incompatible options"
```

**Impact:**  
Bad developer experience. Errors discovered too late.

**Recommendation:**  
Add validation at build time:
```csharp
public ProcessConfiguration Build()
{
    if (UseShellExecution && StandardInput != null)
        throw new InvalidOperationException(
            "Shell execution does not support stdin piping. " +
            "Use direct invocation or remove stdin.");
    // ...
}
```

Or use a builder state machine to prevent invalid states at compile time.

---

## 5. Execution Methods are Confusing

### 5.1 Three Execute Methods with Unclear Decision Tree

**Problem:**  
Three methods exist:
- `ExecuteAsync()` - Non-buffered, streams output?
- `ExecuteBufferedAsync()` - Load all output into memory?
- `ExecutePipedAsync()` - Stream via reader?

When should I use which? No decision matrix. Developers pick wrong and hit deadlocks or memory issues.

**Impact:**  
Common mistakes. Silent failures or mysterious hangs.

**Recommendation:**  
Add a decision flowchart to README:
```
Do you want all output in memory?
  → Yes: ExecuteBufferedAsync()
  → No: Do you need to process output as it comes?
    → Yes: ExecutePipedAsync()
    → No: ExecuteAsync()
```

Also provide a simple convenience method:
```csharp
public Task<string> GetOutputAsync(ProcessConfiguration config)
{
    return ExecuteBufferedAsync(config, ProcessExitConfiguration.Default)
        .ContinueWith(t => t.Result.StandardOutput);
}
```

---

### 5.2 SetStandardInputPipe Semantic is Backwards

**Problem:**  
Method signature:
```csharp
public IProcessConfigurationBuilder SetStandardInputPipe(StreamWriter source)
```

`StreamWriter` wraps an **output** stream, not input. The semantic is backwards. It should be:
```csharp
SetStandardInputSource(Stream source)
```

**Impact:**  
Confusing API. Developers second-guess themselves.

**Recommendation:**  
Rename to `SetStandardInputSource(Stream source)` and handle `StreamWriter` wrapping internally.

---

## 6. Documentation Gaps

### 6.1 README Examples Are Incomplete

**Problem:**  
Examples show happy path only:
```csharp
BufferedProcessResult result = await invoker.ExecuteBufferedAsync(config, ...);
Console.WriteLine(result.StandardOutput);
```

Missing:
- Error handling (`ProcessNotSuccessfulException`)
- How to check exit codes
- Timeout exceptions
- Stream redirection gotchas

**Impact:**  
Developers don't know exceptions are thrown; code crashes in production.

**Recommendation:**  
Add sections:
1. **"Error Handling"** - what exceptions can be thrown, when
2. **"Common Pitfalls"** - "Why is my process hanging?", "Why no output captured?"
3. **"Resource Cleanup"** - disposing credentials, streams, config

---

### 6.2 ProcessExitConfiguration Is Barely Documented

**Problem:**  
README only shows `ProcessExitConfiguration.Default`. No examples of:
- Custom timeouts
- Suppressing exceptions on timeout
- Different behaviors for timeout vs user cancellation

**Impact:**  
Feature is invisible. Users think "configuration" is limited to presets.

**Recommendation:**  
Add complete example:
```csharp
var exitConfig = ProcessExitConfiguration
    .NoTimeoutDefault
    .WithCustomTimeout(TimeSpan.FromSeconds(30))
    .SuppressTimeoutExceptions();
```

(Or whatever the fluent API is.)

---

### 6.3 No Decision Matrix for Execution Methods

**Problem:**  
See section 5.1 above.

**Recommendation:**  
Add flowchart or decision table to README.

---

## 7. API Inconsistencies

### 7.1 Cancellation Token vs ProcessCancellationPolicy Overlap

**Problem:**  
You can pass a `CancellationToken` to `ExecuteAsync()` **and** configure `ProcessCancellationPolicy`. What happens if both fire? Which takes precedence?

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await invoker.ExecuteAsync(config, exitConfig, cts.Token);
// Also: exitConfig might have a timeout of 30 minutes
// Who wins?
```

**Impact:**  
Unclear semantics. Difficult to reason about behavior.

**Recommendation:**  
Document clearly. Consider deprecating one in favor of the other. Ideally:
- `CancellationToken` for user-initiated cancellation
- `ProcessExitConfiguration` timeout for process-level timeout

Don't support both simultaneously; validate and throw if both are provided.

---

### 7.2 No Validation Happens Before Execution

**Problem:**  
Invalid configurations (e.g., non-existent working directory, invalid environment variables) are only discovered during `ExecuteAsync()`:
```csharp
var config = factory.Create("cmd.exe")
    .SetWorkingDirectory("/nonexistent");
// No error yet

await invoker.ExecuteAsync(config);  // ERROR here, not at build
```

**Impact:**  
Late error discovery in async code paths. Hard to debug.

**Recommendation:**  
Add eager validation:
```csharp
public ProcessConfiguration Build()
{
    ValidateWorkingDirectory();
    ValidateCredential();
    ValidateResourcePolicy();
    // throw early
}
```

Or provide a separate validation method:
```csharp
var validationErrors = config.Validate();
if (validationErrors.Any()) throw new InvalidOperationException(...);
```

---

## 8. Missing Convenience Features

### 8.1 No Retry or Conditional Re-Execution Support

**Problem:**  
CliInvoke doesn't help with:
- Retry on timeout
- Retry on specific exit codes
- Fallback to alternative command

Users write boilerplate:
```csharp
for (int i = 0; i < 3; i++)
{
    try
    {
        return await invoker.ExecuteAsync(config, ...);
    }
    catch (ProcessTimeoutException)
    {
        if (i == 2) throw;
    }
}
```

**Impact:**  
Boilerplate code. Common pattern not supported.

**Recommendation:**  
Add extension methods or strategy pattern:
```csharp
await invoker
    .WithRetry(maxAttempts: 3, delayBetweenAttempts: TimeSpan.FromSeconds(1))
    .WithFallback(alternativeCommand)
    .ExecuteAsync(config, ...);
```

---

## 9. Summary: Quick Wins (High ROI)

### Immediate Improvements (1-2 sprints)

1. **Rename ProcessExitConfiguration presets** to be self-documenting (Section 2.3)
2. **Add decision matrix to README** for Execute vs ExecuteBuffered vs ExecutePiped (Section 5.1)
3. **Add error handling section** to README with example (Section 6.1)
4. **Add validation at build time** for unsupported configurations (Section 4.2)
5. **Standardize interface names** everywhere (Section 2.1)

### Medium-Term Improvements (1-2 quarters)

6. **Flatten builder pattern** - mutable intermediate builder or hide constructor (Section 1.1-1.2)
7. **Simplify ProcessCancellationPolicy** - unify cancellation and exception handling (Section 3.3)
8. **Add fluent environment and credential builders** to main builder (Section 4.3)
9. **Document all presets and configuration options** comprehensively (Section 6)
10. **Rename SetStandardInputPipe** to SetStandardInputSource (Section 5.2)

### Long-Term Polish (6+ months)

11. **Create a QuickStart.md** - hello world, common scenarios
12. **Add XML doc examples** to all builder methods
13. **Implement retry/fallback support** as extension methods
14. **Unify stream redirection** with StreamRedirectionMode enum (Section 1.3)
15. **Consider ProcessConfiguration disposal story** (Section 3.2)

---

## 10. Philosophical Notes

CliInvoke is a **solid, feature-complete library**. The problems are not fundamental design flaws but:
- **Complexity not hidden**: Users see 16-parameter constructors instead of fluent builders
- **Naming not intuitive**: "Default" doesn't say what it defaults to
- **Features not discoverable**: Built-in features feel hidden or optional
- **Validation too late**: Errors at runtime instead of build time

The library would benefit from applying these principles:
1. **Hide complexity behind fluent APIs**
2. **Name things for clarity, not brevity**
3. **Validate early, fail fast**
4. **Document decision trees, not just APIs**
5. **Provide convenience methods for common scenarios**

---

## Appendix: Example of Improved Builder API

Before:
```csharp
var config = new ProcessConfigurationBuilder("dotnet")
    .SetArguments("build")
    .SetWorkingDirectory("/src")
    .SetEnvironmentVariables(envDict)
    .Build();

var exitConfig = ProcessExitConfiguration.Default;
var result = await invoker.ExecuteBufferedAsync(config, exitConfig, cancellationToken);
```

After (proposed):
```csharp
var config = ProcessConfiguration
    .For("dotnet")
    .WithArguments("build")
    .InWorkingDirectory("/src")
    .WithEnvironmentVariables(env => env
        .SetPair("DOTNET_CLI_TELEMETRY_OPTOUT", "1"))
    .Build();

var result = await invoker.ExecuteAsync(config)
    .RequireZeroExitCode()
    .WithTimeout(TimeSpan.FromMinutes(5))
    .SuppressTimeoutException();
```

This is more discoverable, self-documenting, and fluent.

---

## Conclusion

CliInvoke is well-engineered and solves a real problem. With the improvements outlined here—especially around naming clarity, early validation, and documentation—adoption would increase significantly and the API would feel modern and intuitive.
