## WCountLib.Providers.wc Library

This package provides implementations for WCountLib.Abstractions interfaces that use Posix's and Unix's `wc` program to perform the calculations.

The functionality in this package relies on the `wc` program, which is available on Unix-based operating systems (macOS, Linux, FreeBSD, and Mac Catalyst).

### Notes
This library is built on **CliInvoke v3**. Consumers must register CliInvoke with Dependency Injection before using the counters.

For apps using `Microsoft.Extensions.DependencyInjection` or `Microsoft.Extensions.Hosting`, install `CliInvoke.Extensions` and call the `AddCliInvoke` service collection extension method. This registers `IProcessInvoker`, `IFilePathResolver`, and the rest of CliInvoke's services:

```csharp
using CliInvoke.Extensions;
using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();
services.AddCliInvoke(); // registers IProcessInvoker, IFilePathResolver, etc.

IServiceProvider provider = services.BuildServiceProvider();

IWordCounter counter = new WcWordCounter(
    provider.GetRequiredService<IProcessInvoker>(),
    provider.GetRequiredService<IFilePathResolver>());
```

Internally, each counter builds its `wc` invocation with the v3 `ProcessConfigurationBuilder` and the `ArgumentsSpec` configuration seam, resolves the `wc` executable path via `IFilePathResolver`, and runs it through `IProcessInvoker.ExecuteBufferedAsync`. `ProcessConfiguration` is not disposable; disposal of any `StandardInput` or `UserCredential` it references remains the caller's responsibility.

### Supported Platforms
The following table details which target platforms are supported for accessing WCountLib functionality via `wc`.

| Operating System | Support Status                     | Notes                                                                        |
|------------------|------------------------------------|------------------------------------------------------------------------------|
| Windows          | Not supported :x:                  |                                                                              |
| macOS            | Fully Supported :white_check_mark: |                                                                              |
| Mac Catalyst     | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work. |
| Linux            | Fully Supported :white_check_mark: |                                                                              |
| FreeBSD          | Fully Supported :white_check_mark: |                                                                              |
| Android          | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work. |
| IOS              | Not Supported :x:                  |                                                                              | 
| tvOS             | Not Supported :x:                  |                                                                              |
| watchOS          | Not Supported :x:                  |                                                                              |


### Licensing
This library is licensed under the MIT licence.

If you'd like to contribute to the project, please visit the [GitHub Repo](https://github.com/alastairlundy/WCount/).
