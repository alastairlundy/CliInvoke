## How to Build CliInvoke's code

### Requirements
CliInvoke requires the latest .NET release SDK to be installed to target all supported TFM (Target Framework Moniker) build targets.

Currently, the required .NET SDK is .NET 9. 

The current build targets include:
* .NET Standard 2.0 
* .NET 8
* .NET 9

Any version of the .NET 9 SDK can be used, but using the latest version is preferred.

### Versioning new releases
CliInvoke aims to follow Semantic versioning with ```[Major].[Minor].[Build]``` for most circumstances and an optional ``.[Revision]`` when only a configuration change is made, or a new build of a preview release is made.

#### Pre-releases
Pre-release versions should have a suffix of -alpha, -beta, -rc, or -preview followed by a ``.`` and what pre-release version number they are. The number should be incremented by 1 after each release unless it only contains a configuration change, or another packaging, or build change. An example pre-release version may look like 1.1.0-alpha.2 , this version string would indicate it is the 2nd alpha pre-release version of 1.1.0 .

#### Stable Releases
Stable versions should follow semantic versioning and should only increment the Revision number if a release only contains configuration or build packaging changes, with no change in functionality, features, or even bug or security fixes.

Releases that only implement bug fixes should see the Build version incremented.

Releases that add new non-breaking changes should increment the Minor version. Minor breaking changes may be permitted in Minor version releases where doing so is necessary to maintain compatibility with an existing supported platform, or an existing piece of code that requires a breaking change to continue to function as intended.

Releases that add major breaking changes or significantly affect the API should increment the Major version. Major version releases should not be released with excessive frequency and should be released when there is a genuine need for the API to change significantly for the improvement of the project.

### Building for Testing
You can build for testing by building the desired project within your IDE or VS Code, or manually by entering the following command: ``dotnet build -c Debug``.

If you encounter any bugs or issues, try running the ``CliInvoke.Tests`` project and setting breakpoints in the affected CliInvoke project's code where appropriate. Failing that, please [report the issue](https://github.com/alastairlundy/CliInvoke/issues/new/) if one doesn't already exist for the bug(s).

### Building for Release
Before building a release build, ensure you apply the relevant changes to the relevant ``.csproj`` file corresponding to the package you are trying to build:
* Update the Package Version variable 
* Update the project file's Changelog
* Remove/replace the CliInvoke icon if distributing a non-official release build to a wider audience. See [CliInvoke Assets](#CliInvoke-assets) for more details.

You should ensure the project builds under debug settings before producing a release build.

#### Producing Release Builds
To manually build a project for release, enter ``dotnet build -c Release /p:ContinuousIntegrationBuild=true`` for a release with [SourceLink](https://github.com/dotnet/sourcelink) enabled or just ``dotnet build -c Release`` for a build without SourceLink.

Builds should generally always include Source Link and symbol packages if intended for wider distribution.

## Licensing
CliInvoke.Extensions is licensed under the MPL 2.0 license. If you modify any of the package's files then the modified files must remain licensed under the MPL 2.0 .

If you use this package in your project please make an exact copy of the contents of the LICENSE.txt file available either in your third party licenses txt file or as a separate txt file.

### Assets
CliInvoke's Icon is NOT licensed under the MPL 2.0 license and are licensed under Copyright with all rights reserved to me (Alastair Lundy).

If you fork CliInvoke and re-distribute it, please replace the usage of the icon unless you have prior written agreements from me.
