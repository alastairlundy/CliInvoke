# CliInvoke v1 to v2 Migration Guide.

CliInvoke v2 contains a number of breaking changes from CliInvoke v1. This document aims to detail alternatives to removed interfaces and classes.

## v2 Overview
CliInvoke version 2, hereafter v2, is a redesign of CliInvoke that vastly simplifies the API surface, improves the ergonomic experience of using CliInvoke, and adds support for Process Exit Configuration and additional features.

Explicit support for .NET Standard 2.1 has been removed and is now provided through .NET Standard 2.0 Support

### Supported Target Frameworks
CliInvoke v2 supports the following target frameworks:
* .NET Standard 2.0
* .NET 8
* .NET 9

There is implicit support for .NET 10 but explicit support will come in a future update.

## Breaking Changes Summary

* [Removals](#removals)
* [Method Signature Changes](#method-signature-changes)

### Removals

#### ``IProcessRunnerUtility`` and ``ProcessRunnerUtility``
**Rationale**:
* Although well intentioned, the interface and classes were badly designed, exposed most of the shortcomings of .NET's ``Process`` class, and were neither helpful to CliInvoke's design nor were they helpful to users.

**Replacement**:
* There are no direct replacements for the reasons stated in the rationale section. Use ``IProcessInvoker`` and ``ProcessInvoker`` instead.

#### ``IPipedProcessRunner`` and ``PipedProcessRunner``
**Rationale**:  
* Although well intentioned, the interface and classes were badly designed, and are no longer needed as a middleman between Process Invokers and Process ProcessRunnerUtility. It contributed to a more fragmented and confused API surface and thus removal was necessary to clean up the CliInvoke v2 API Surface.

**Replacement**: 
* ``IProcessInvoker`` and ``ProcessInvoker`` for running ``ProcessConfiguration`` objects.


#### ``IProcessFactory`` and ``ProcessFactory``
**Rationale**:  Removed due to exposing behaviour and functionality that is antithetical to the goals of CliInvoke. Safe process running and avoiding the pitfalls of the ``Process`` class are easily done with other invokers and ``IProcessFactory``'s interface methods made it easier to run into some of these pitfalls.

**Replacement**: 
* ``IProcessConfigurationFactory`` and ``ProcessConfigurationFactory`` for simple ``ProcessConfiguration`` creation.
* ``IProcessInvoker`` and ``ProcessInvoker`` for running ``ProcessConfiguration`` objects.

#### ``ICliCommandInvoker`` and ``CliCommandInvoker``
**Rationale**: 
* The abstractions around CliInvoke have become less Command centric and more external process centric and as such a name change was warranted. Alongside this, breaking changes were needed for new features and so ``IProcessInvoker ``and ``ProcessInvoker`` were introduced as the successor to `ICliCommandInvoker`` and ``CliCommandInvoker`` respectively.


**Replacement**: 
* ``IProcessInvoker`` and ``ProcessInvoker`` respectively


#### ``CliCommandConfiguration``

**Rationale**:
* The abstractions around CliInvoke have become less Command centric and more external process centric and as such a name change was warranted. Alongside this, some breaking changes were needed for several features and so ``ProcessConfiguration `` was introduced as the successor to ``CliCommandConfiguration``.

**Replacement**:
* ``ProcessConfiguration``

### Method Signature Changes
These cover common use cases and are NOT an exhaustive list of method signature changes.

#### IProcessInvoker and ProcessInvoker

##### ExecuteAsync
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


##### ExecuteBufferedAsync
**v1 Method Signature**:
```csharp
Task<BufferedProcessResult> ExecuteBufferedProcessAsync(Process process, ProcessConfiguration processConfiguration,CancellationToken cancellationToken = default);
```

##### ExecuteBufferedAsync
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

**Removed methods**:

##### ExecuteProcessAsync

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

##### ExecuteBufferedProcessAsync
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

#### IFilePathResolver and FilePathResolver

##### ResolveFilePath

**v1 Method Signature**
```csharp

void ResolveFilePath(string inputFilePath, out string outputFilePath);
```

**v2 Method Signature**
```csharp
string ResolveFilePath(string filePathToResolve);
```

##### TryResolveFilePath

**v2**
Removed in CliInvoke v2. Use ``ResolveFilePath`` within a ``try/catch`` if a FileNotFound exception, in case of file path not being resolvable, being thrown is undesirable; otherwise use ``ResolveFilePath``.

#### IArgumentsBuilder and ArgumentsBuilder

#### IArgumentsBuilder and ArgumentsBuilder

**Method Signature Chanes**:

##### Add Enumerable with bool

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<string> values, bool escapeSpecialChars);
```

**v2 Method Signature**:

##### Add Enumerable

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IEnumerable<string> values);
```

**v2 Method Signature**:

##### Add Enumerable with IFormattable

**v1 Method Signature**:
```csharp
IArgumentsBuilder Add(IFormattable value, IFormatProvider formatProvider, bool escapeSpecialChars = true);
```

**v2 Method Signature**:


**Removed Methods**:


#### IProcessConfigurationBuilder and ProcessConfigurationBuilder

#### IEnvironmentVariablesBuilder and EnvironmentVariablesBuilder

#### IProcessResourcePolicyBuilder and ProcessResourcePolicyBuilder

##### Set string pair
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(string name, string value);
```

**v2 Method Signature**:
```csharp

```

##### Set variables from Enumerable
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IEnumerable<KeyValuePair<string, string>> variables);
```

**v2 Method Signature**:


##### Set variables from IReadOnlyDictionary<string, string>
**v1 Method Signature**:
```csharp
IEnvironmentVariablesBuilder Set(IReadOnlyDictionary<string, string> variables);
```

**v2 Method Signature**:


#### IProcessResourcePolicyBuilder and ProcessResourcePolicyBuilder

##### Setting Processor Affinity
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithProcessorAffinity(nint processorAffinity);
```

**v2 Method Signature**:

##### Setting Minimum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMinWorkingSet(nint minWorkingSet);
```

**v2 Method Signature**:

##### Setting Maximum Working Set
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithMaxWorkingSet(nint maxWorkingSet);
```

**v2 Method Signature**:


##### Setting Priority Class
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityClass(ProcessPriorityClass processPriorityClass);
```

**v2 Method Signature**:


##### Setting Priority Boost
**v1 Method Signature**:
```csharp
IProcessResourcePolicyBuilder WithPriorityBoost(bool enablePriorityBoost);
```

**v2 Method Signature**:


#### IProcessPipeHandler and ProcessPipeHandler

##### PipeStandardInputAsync

**v1 Method Signature**


**v2 Method Signature**
    ```csharp
    Task<bool> PipeStandardInputAsync(Stream source, Process destination);
    ```

**v1 Method Signature**:
```csharp
Task PipeStandardInputAsync(Stream source, Process destination);
```

**v2 Method Signature**:
```csharp
Task<bool> PipeStandardInputAsync(Stream source, Process destination);
```

##### PipeStandardOutputAsync

**v1 Method Signature**


**v2 Method Signature**
    ```csharp
        Task<Stream> PipeStandardOutputAsync(Process source);
    ```
```csharp
Task PipeStandardOutputAsync(Process source, Stream destination);
```

**v2 Method Signature**
```csharp
Task<Stream> PipeStandardOutputAsync(Process source);
```

##### PipeStandardErrorAsync

**v1 Method Signature**


**v2 Method Signature**
    ```csharp
    Task<Stream> PipeStandardErrorAsync(Process source);
    ```
```csharp
Task PipeStandardErrorAsync(Process source, Stream destination);
```

**v2 Method Signature**
```csharp
Task<Stream> PipeStandardErrorAsync(Process source);
```

## Migration Steps
For CliInvoke v1 users not using the latest version of v1, update to the latest version of CliInvoke v1.

1. Ensure your project works and builds successfully with CliInvoke v1.
2. Create a new branch for updating to CliInvoke v2.
3. Replace ``ICliCommandInvoker`` usage with ``IProcessInvoker`` and ``CliCommandConfiguration`` usage with ``ProcessConfiguration``.
4. Replace usage of all other deprecated code with newer alternatives or replacements where available.
5. Update to CliInvoke v2.
6. Address breaking changes in method signatures and elsewhere.
7. Resolve all remaining errors.
8. Update your project's testing code as needed to work with v2.
8. Test that the project still works and builds successfully (this time with v2).
9. Merge into your project's main branch if successful.