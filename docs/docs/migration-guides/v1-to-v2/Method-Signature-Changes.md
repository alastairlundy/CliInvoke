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

### Add with IFormattable and IFormatProvider

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escapeSpecialChars = true);
```

**v2 Method Signature**:
```csharp
IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, string? format = null, bool escapeSpecialChars = true);
```

### Add Enumerable with IFormattable and IFormatProvider

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<IFormattable> values, IFormatProvider formatProvider, bool escapeSpecialChars = true);
```

**v2 Method Signature**:
```csharp
IArgumentsBuilder AddEnumerable(IEnumerable<IFormattable> values, IFormatProvider formatProvider,  string? format = null, bool escapeSpecialChars = true);
```

### Escape Characters
**v1 Method Signature**:
```csharp
string EscapeSpecialCharacters(string argument);
```

**v2 Method Signature**:
```csharp
string EscapeCharacters(string argument);
```

## IProcessConfigurationBuilder and ProcessConfigurationBuilder

### Set Arguments string
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithArguments(string arguments);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetArguments(string arguments);
```

### Set Arguments IEnumerable<string>
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetArguments(IEnumerable<string> arguments);
```

### Set Arguments IEnumerable<string> with Argument Escaping
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithArguments(IEnumerable<string> arguments, bool escapeArguments);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetArguments(IEnumerable<string> arguments, bool escapeArguments);
```

### Set Environment Variables
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetEnvironmentVariables(IReadOnlyDictionary<string, string> environmentVariables);
```

### Set Target File Path
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithTargetFilePath(string targetFilePath);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetTargetFilePath(string targetFilePath);
```


### Set User Credential
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithUserCredential(UserCredential credentials);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetUserCredential(UserCredential credentials);
```

### Set User Credential with Action<IUserCredentialBulder>
**v1 Method Signature**:
```csharp
IProcessConfigurationBuilder WithUserCredential(Action<IUserCredentialBuilder> configure);
```

**v2 Method Signature**:
```csharp
IProcessConfigurationBuilder SetUserCredential(Action<IUserCredentialBuilder> configure);
```

## IEnvironmentVariablesBuilder and EnvironmentVariablesBuilder

### Set string pair
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(string name, string value);
```

**v2 Method Signature**:
```csharp
IEnvironmentVariablesBuilder SetPair(string name, string value);
```

### Set variables from Enumerable
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string>> variables);
```

**v2 Method Signature**:
```csharp
IEnvironmentVariablesBuilder SetEnumerable(IEnumerable<KeyValuePair<string, string>> variables);
```

### Set variables from IReadOnlyDictionary<string, string>
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string> variables);
```

**v2 Method Signature**:
```csharp
IEnvironmentVariablesBuilder SetReadOnlyDictionary(IReadOnlyDictionary<string, string> variables);
```

## IProcessResourcePolicyBuilder and ProcessResourcePolicyBuilder

### Setting Processor Affinity
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithProcessorAffinity(nint processorAffinity);
```

**v2 Method Signature**:
```csharp
IProcessResourcePolicyBuilder SetProcessorAffinity(nint processorAffinity);
```

### Setting Minimum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMinWorkingSet(nint minWorkingSet);
```

**v2 Method Signature**:
```csharp
IProcessResourcePolicyBuilder SetMinWorkingSet(nint minWorkingSet);
```

### Setting Maximum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMaxWorkingSet(nint maxWorkingSet);
```

**v2 Method Signature**:
```csharp
IProcessResourcePolicyBuilder SetMaxWorkingSet(nint maxWorkingSet);
```

### Setting Priority Class
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityClass(ProcessPriorityClass processPriorityClass);
```

**v2 Method Signature**:
```csharp
IProcessResourcePolicyBuilder SetPriorityClass(ProcessPriorityClass processPriorityClass);
```

### Setting Priority Boost
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityBoost(bool enablePriorityBoost);
```

**v2 Method Signature**:
```csharp
IProcessResourcePolicyBuilder ConfigurePriorityBoost(bool enablePriorityBoost);
```

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