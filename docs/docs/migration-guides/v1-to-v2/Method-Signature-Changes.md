# Method Signature Changes
These cover common use cases and are NOT an exhaustive list of method signature changes.

## IProcessInvoker and ProcessInvoker

### ExecuteAsync
**v1 Method Signature**:
```csharp
Task<ProcessResult> ExecuteProcessAsync(Process process, ProcessConfiguration processConfiguration,CancellationToken cancellationToken = default);
```

**v2 Method Signature**:
```csharp
Task<ProcessResult> ExecuteAsync(
    ProcessConfiguration processConfiguration,
    ProcessExitConfiguration? processExitConfiguration = null,
    bool disposeOfConfig = false,
    CancellationToken cancellationToken = default);
```


### ExecuteBufferedAsync
**v1 Method Signature**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process, ProcessConfiguration processConfiguration,CancellationToken cancellationToken = default);
```

### ExecuteBufferedAsync
**v1 Method Signature**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process,
ProcessConfiguration processConfiguration,
CancellationToken cancellationToken = default);
```

**v2 Method Signature**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedAsync(
    ProcessConfiguration processConfiguration,
    ProcessExitConfiguration? processExitConfiguration = null,
    bool disposeOfConfig = false,
    CancellationToken cancellationToken = default);
```

## IFilePathResolver and FilePathResolver

### ResolveFilePath

**v1 Method Signature**
```csharp

void ResolveFilePath(string inputFilePath, out string outputFilePath);
```

**v2 Method Signature**
```csharp
string ResolveFilePath(string filePathToResolve);
```

## IArgumentsBuilder and ArgumentsBuilder

## IArgumentsBuilder and ArgumentsBuilder

**Method Signature Chanes**:

### Add Enumerable with bool

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<string> values, bool escapeSpecialChars);
```

**v2 Method Signature**:

### Add Enumerable

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<string> values);
```

**v2 Method Signature**:

### Add Enumerable with IFormattable

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escapeSpecialChars = true);
```

**v2 Method Signature**:



## IProcessConfigurationBuilder and ProcessConfigurationBuilder

## IEnvironmentVariablesBuilder and EnvironmentVariablesBuilder

## IProcessResourcePolicyBuilder and ProcessResourcePolicyBuilder

### Set string pair
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(string name, string value);
```

**v2 Method Signature**:
```csharp

```

### Set variables from Enumerable
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string>> variables);
```

**v2 Method Signature**:


### Set variables from IReadOnlyDictionary<string, string>
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string> variables);
```

**v2 Method Signature**:


## IProcessResourcePolicyBuilder and ProcessResourcePolicyBuilder

### Setting Processor Affinity
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithProcessorAffinity(nint processorAffinity);
```

**v2 Method Signature**:

### Setting Minimum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMinWorkingSet(nint minWorkingSet);
```

**v2 Method Signature**:

### Setting Maximum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMaxWorkingSet(nint maxWorkingSet);
```

**v2 Method Signature**:


### Setting Priority Class
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityClass(ProcessPriorityClass processPriorityClass);
```

**v2 Method Signature**:


### Setting Priority Boost
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityBoost(bool enablePriorityBoost);
```

**v2 Method Signature**:


## IProcessPipeHandler and ProcessPipeHandler

### PipeStandardInputAsync
**v1 Method Signature**:
```csharp
Task PipeStandardInputAsync(Stream source, Process destination);
```

**v2 Method Signature**:
```csharp
Task<bool> PipeStandardInputAsync(Stream source, Process destination);
```

### PipeStandardOutputAsync

**v1 Method Signature**
```csharp
Task PipeStandardOutputAsync(Process source, Stream destination);
```

**v2 Method Signature**
```csharp
Task<Stream> PipeStandardOutputAsync(Process source);
```

### PipeStandardErrorAsync

**v1 Method Signature**
```csharp
Task PipeStandardErrorAsync(Process source, Stream destination);
```

**v2 Method Signature**
```csharp
Task<Stream> PipeStandardErrorAsync(Process source);
```