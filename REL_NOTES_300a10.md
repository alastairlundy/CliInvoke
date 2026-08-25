## Changes since 3.0.0-alpha.9

### All Projects

#### ⚙️ Modifications

##### CI Dependencies

- Updated Directory.Packages.props for NuGet Central Package Management
- Bumped github/codeql-action/upload-sarif from 4.37.7 to 4.37.8

### CliInvoke.Core

#### 🆕 Additions

- Made `Configuration` init-only on `IExternalProcess` and `ExternalProcess`
- Made `TargetFilePath` init-only and updated `ProcessWrapper` constructor

#### ⚙️ Modifications

- Collapsed `ProcessConfigurationFactory` to two static spec-callback overloads
- Deleted `BuilderProcessConfiguration` bridge subclass
- Resolved file path at `ExternalProcess.Start`/`StartAsync` without mutating `Configuration`
- Sealed `ProcessConfigurationBuilder`
- Sorted environment variables by key in `GetHashCode` for ordering independence
- Removed LINQ usage in `ProcessConfiguration`
- Improved `MiddlewareItems` performance
- Improved `ProcessConfiguration.Equals` performance
- Updated `MiddlewareItems.cs`
- Added XML doc comment to `ProcessConfiguration`
- Updated `PipedProcessResult.cs`
- Cleaned up code
- Cleaned up `ProcessConfigurationBuilder` and `ProcessConfigurationFactory`
- Removed unnecessary casting code
- Added localizations to internal resources
- Changed 15-param `ProcessConfiguration` constructor visibility from `protected` to `protected internal`
- Documented `InvocationContext.Result` and `.Middleware` ownership contract

#### 🐛 Bug Fixes

- Fixed `ProcessResult` equality asymmetry
- Fixed code smells: null safety, inverted condition, null equality, dead override, dict equality
- Fixed XML doc comments for comparison operator null ordering
- Fixed outdated equality remarks in `BufferedProcessResult` and `PipedProcessResult`
- Fixed `BufferedProcessResult` equals method
- Fixed `TargetFilePath` XML remarks to say "after construction"
- Fixed copyright header and XML doc in `ProcessTimeoutPolicy`

### CliInvoke

#### ⚙️ Modifications

- Stripped `CliRun` static mutable state
- Reduced `ProcessInvoker` constructors
- Tightened `ExternalProcess` public API
- Reduced constructor code duplication
- Moved duplicated `CliRun` code to shared helper
- Updated `ProcessInvoker.cs`
- Updated `ProcessInvoker` to reflect `MiddlewareChain` not being null
- Updated `ProcessConfigurationFactory.cs`
- Migrated 21 params-overload call sites to new two-overload factory API
- Replaced stale TODO in `ProcessInvocationPipeline` with design note
- Clarified TODO in `CliRun.GetPipeline` about Rider volatile warning

#### 🐛 Bug Fixes

- Fixed `ExternalProcess` to dispose old wrapper and reattach event handlers

### CliInvoke.Specializations

#### 🆕 Additions

- Dropped `IFilePathResolver` from `PowershellProcessConfiguration` constructor

#### ⚙️ Modifications

- Dropped `IFilePathResolver` from `PowershellProcessInvoker` and `PowerShellMiddleware`

### CliInvoke.Extensions

#### ⚙️ Modifications

- Updated DI binding to use the reduced `ProcessInvoker` constructors
- Updated DI registration to construct `PowerShellMiddleware` without `IFilePathResolver`

#### 📄 Non Source Code

- Fixed stale v1 API references in `CliInvoke.Extensions` README