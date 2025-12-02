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

## IProcessConfigurationBuilder and ProcessConfigurationBuilder

### Setting Standard Input, Standard Output, and Standard Error
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithEncoding(Encoding? standardInputEncoding = null,
Encoding? standardOutputEncoding = null,
Encoding? standardErrorEncoding = null);
```

**Replacements**:
For setting Standard Input Encoding:
```csharp
IProcessConfigurationBuilder SetStandardInputEncoding(Encoding? standardInputEncoding = null);
```

For setting Standard Output Encoding:
```csharp
IProcessConfigurationBuilder SetStandardOutputEncoding(Encoding? standardOutputEncoding = null);
```

For setting Standard Error Encoding:
```csharp
IProcessConfigurationBuilder SetStandardErrorEncoding(Encoding? standardErrorEncoding = null);
```

### Setting ProcessResultValidation
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithValidation(ProcessResultValidation validation);
```

**Replacement**:
Configure a ProcessExitConfiguration with ProcessResultValidation.

The relevant ``ProcessExitConfiguration`` constructor is the following:
```csharp
public ProcessExitConfiguration( ProcessTimeoutPolicy timeoutPolicy, ProcessResultValidation resultValidation,
ProcessCancellationExceptionBehavior cancellationValidation)
```

Use ``ProcessExitConfiguration`` with ``IProcessInvoker`` as a parameter in ``ExecuteAsync``, ``ExecuteBufferedAsync``, or ``ExecutePipedAsync``.

## IArgumentsBuilder and ArgumentsBuilder

### Add with Formatable and Format Provider and Culture Info
**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IFormattable value, CultureInfo cultureInfo, bool escapeSpecialChars);
```

**v2 Replacements**:
```csharp
IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values);
```

### Add Enumerable with Formatable and Format Provider and Culture Info
**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<IFormattable> values, CultureInfo cultureInfo, bool escapeSpecialChars);
```

**v2 Replacements**:
```csharp
IArgumentsBuilder Add(IFormattable value);
```