## Supported Operating Systems
CliInvoke can currently be added to .NET Standard 2.0, .NET 8, or .NET 9 or newer supported projects.

The following table details which target platforms are supported for executing commands via CliInvoke. 

| Operating System | Support Status                     | Notes                                                                                       |
|------------------|------------------------------------|---------------------------------------------------------------------------------------------|
| Windows          | Fully Supported :white_check_mark: |                                                                                             |
| macOS            | Fully Supported :white_check_mark: |                                                                                             |
| Mac Catalyst     | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| Linux            | Fully Supported :white_check_mark: |                                                                                             |
| FreeBSD          | Fully Supported :white_check_mark: |                                                                                             |
| Android          | Untested Platform :warning:        | Support for this platform has not been tested but should theoretically work.                |
| IOS              | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting IOS. <sup>3</sup>                   | 
| tvOS             | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting tvOS <sup>3</sup>                   |
| watchOS          | Not Supported :x:                  | Not supported due to ``Process.Start()`` not supporting watchOS <sup>4</sup>                |
| Browser          | Not Planned :x:                    | Not supported due to not being a valid target Platform for executing programs or processes. |

<sup>3</sup> - See the [Process class documentation](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start?view=net-9.0#system-diagnostics-process-start) for more info.

<sup>4</sup> - Lack of watchOS support is implied by lack of IOS support since [watchOS is based on IOS](https://en.wikipedia.org/wiki/WatchOS).

**Note:** This library has not been tested on Android or Tizen.
