# Changelog Archive

Stable releases prior to 2.0. For current releases, see [CHANGELOG.md](CHANGELOG.md).

## [1.6.1.1] - 2025-10-14

### Fixed
- Fixed an issue where `FilePathResolver` would fail to correctly resolve a file path
- Fixed several issues where exceptions would be thrown upon exiting a Process
- Fixed an issue where a short running Process could throw an exception if setting Process Resource Policy was attempted

## [1.6.1] - 2025-10-09

### Changed
- Updated DotExtensions from 7.7.0 to 8.6.2 for .NET Standard 2.0 TFM
- Updated DotExtensions from 8.5.1 to 8.6.2 for .NET 8+ TFMs

### Deprecated
- (Core package) Deprecated `IProcessFactory` for removal in v2
- (Main package) Deprecated `ProcessFactory` for removal in v2

## [1.6.0] - 2025-10-01

### Added
- Added constructor overload for ProcessRunner class that doesn't rely on the deprecated IProcessRunnerUtility interface
- Added FilePathResolver.cs directly into CliInvoke since Resyslib.IO is no longer maintained. This is a backport of CliInvoke v2's File Path Resolving logic but in the same interface that v1 was using.

### Changed
- Updated DotExtensions on .NET 8 and newer from 8.3.0 to 8.5.0
- Updated internal Polyfill usage on .NET Standard 2 from 8.8.1 to 8.9.0

### Deprecated
- Deprecated some extension methods and constructors in classes for removal in v2.
- Deprecated CliCommandInvoker class for removal in v2. The intended replacement in v2 is ProcessInvoker.

### Fixed
- Fixed an issue where CliInvoke for .NET 10 (as an implicit TFM) would depend on DotExtensions 7.7.0 instead of 8.x

## [1.5.2] - 2025-09-16

### Changed
- Updated DotExtensions from 7.6.5 to 7.7.0 on .NET Standard 2.0
- Updated DotExtensions from 8.2.0 to 8.3.0 on .NET 8 and .NET 9
- Updated internal Polyfill usage from 8.8.0 to 8.8.1

### Deprecated
- Marked some interfaces as deprecated for removal in v2

## [1.5.1] - 2025-08-25

### Changed
- Updated DotExtensions from 7.6.2 to 7.6.5 on .NET Standard 2.0
- Updated DotExtensions from 7.6.2 to 8.2.0 on .NET 8 and .NET 9
- Updated internal Polyfill usage from 8.7.3 to 8.8.0
- Moved some abstractions to CliInvoke.Core
- Updated to CliInvoke.Core 1.5.1

## [1.5.0] - 2025-07-30

### Added
- Added `ProcessInvoker` the replacement for `ProcessRunner`. Please use `ProcessInvoker` instead going forward.

### Changed
- Updated DotExtensions from 7.5.1 to 7.6.2
- Updated internal Polyfill usage from 8.7.0 to 8.7.3
- Moved `ProcessFactory` from its Legacy namespace into the main CliInvoke namespace
- Moved `ProcessPipeHandler` from its Legacy namespace into the `CliInvoke.Piping` namespace

### Deprecated
- Deprecated `ProcessRunner` class
- Deprecated `ICommandProcessFactory` interface

## [1.4.5] - 2025-07-10

### Changed
- Updated to DotExtensions 7.4.2 from 7.4.1
- Updated internal Polyfill version from 8.4.0 to 8.7.0
- Updated System.IO.Pipelines from 9.0.6 to 9.0.7
- Update to CliInvoke.Core 1.4.5

## [1.4.4] - 2025-07-07

### Changed
- Updated DotExtensions from 7.2.1 to 7.4.1
- Updated internal Polyfill usage from 8.0.1 to 8.4.0

## [1.4.3] - 2025-06-23

### Changed
- Aligned Core and Main package versions (skipped 1.4.2 to align both at 1.4.3)
- Updated DotExtensions from 7.1.1 to 7.2.1, and updated other dependencies

## [1.4.1] - 2025-06-11

### Changed
- Hide CliInvoke's dependency on Polyfill

## [1.4.0] - 2025-06-09

### Added
- Add simple exec to basic benchmark comparison
- Add Basic Benchmark comparing CliInvoke to other alternatives

### Changed
- Update to CliInvoke.Core 1.4.1 and Resyslib.IO 3.1.0
- Update DotExtensions from 6.8.0 to 7.1.1, and Polyfill from 7.31.0 to 7.33.0

### Fixed
- Add missing `ProcessConfigurationBuilder` to CliInvoke

## [1.3.1] - 2025-05-23

### Changed
- Update CliInvoke to use IO Extensions Abstractions 3.0 instead of direct implementation

### Fixed
- Update Extensions.IO to 3.0.1 to fix File Path Resolving issues
- Fixed an issue where ProcessFactory unintentionally tries to redirect Standard Input after Process has started

## [1.3.0] - 2025-05-19

### Changed
- Update Polyfill dependency to 7.31.0 and update DotExtensions to 6.8.0
- Update CliInvoke code to work with CliInvoke.Core

## [1.2.1] - 2025-05-19

### Fixed
- Fixed an issue with Command Specialization Configurations

## [1.2.0] - 2025-04-08

### Changed
- Update Polyfill dependency and replace SystemExtensions dependency with DotExtensions dependency

## [1.1.0] - 2025-03-18

### Changed
- Update dependencies

### Deprecated
- Deprecate CommandProcessFactory `CreateProcess` method

## [1.0.0] - 2025-03-02

### Added
- Add DocFx documentation

### Fixed
- Corrected xml doc comment issues
