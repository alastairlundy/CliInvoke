---
title: Comparison vs Alternatives
layout: simple
---

# Comparison vs Alternatives

| Feature / Criterion                                                        |  CliInvoke   |                                  [CliWrap](https://github.com/Tyrrrz/CliWrap/)                                   |    [ProcessX](https://github.com/Cysharp/ProcessX)     |                             .NET Process class                             |
|----------------------------------------------------------------------------|:------------:|:----------------------------------------------------------------------------------------------------------------:|:------------------------------------------------------:|:--------------------------------------------------------------------------:|
| Separate configuration and invocation types                               |      ✅      |   ❌ (`Command` is a partial class — configuration and execution share one type) |                           �                           | ⚠️ (`ProcessStartInfo` is a config model; execution uses `Process` itself) |
| Builder pattern / fluent configuration API                                | ✅ (`ProcessConfigurationBuilder`, `IExternalProcessFactory`) | ✅ (dedicated builder classes; fluent `Command` chain via `Cli.Wrap(...)) | ✅ (per-command configuration object — see project docs for details) | ❌ (no builder; consumers construct `ProcessStartInfo` directly) |
| Dependency Injection registration extensions                               |      ✅      |                                                        ❌                                                        |                           ❌                           |                                     ❌                                     |
| Installable via NuGet                                                      |      ✅      |                                                        ✅                                                        |                           ✅                           |                            ✅ , Built into .NET                            |
| Official cross‑platform support (advertised: Windows/macOS/Linux/BSD)      |      ✅      |                            ✅ (Windows/macOS/Linux officially; BSD unverified)                                    |            ❌ (Windows‑only officially)              |                                     ✅                                     |
| Buffered and non‑buffered execution modes                                  |      ✅      |                                                        ✅                                                        |                           ✅                           |      ⚠️, available; drain stdout and stderr concurrently to avoid deadlocks      |
| Result type variants                                                       |  ✅ (ProcessResult / BufferedProcessResult) |  ✅ (CommandResult / BufferedCommandResult)            |            ✅ (multiple result shapes)               | ⚠️ (`ProcessExitInfo` in .NET 11 Preview only; sealed struct, no buffered/piped variants) |
| Support for Process/Command Timeout                                        |      ✅      |                              :warning:, limited to cancelling via CancellationToken                              | :warning:, limited to cancelling via CancellationToken |           :warning:, limited to cancelling via CancellationToken           |
| Graceful Cancellation Support via SIGTERM/SIGINT Signals                   |  ✅, 2.3.0+  |          ⚠️, requires bundled .NET Framework console helper on Windows                                          |                           ❌                           |                                     ❌                                     |
| Small surface area and minimal dependencies                                |      ✅      |                                                        ✅                                                        |                           ✅                           |                                     ✅                                     |
| Middleware / cross-cutting pipeline                                       | ✅ (v3 pre-release; `IProcessMiddleware` chain via `ProcessInvoker`) | ❌ | ❌ | ❌ |
| License                                                                    |     MPL‑2.0     |                                  MIT                                                                              |                         MIT                          |                     MIT (.NET Runtime)                                  |
| Fork / maintenance notes                                                   | MPL‑2.0 file‑level copyleft — retain MPL notice on copied files  | Test projects depend on a source‑available (non‑OSI) library; check its license before redistributing the test suite | MIT, no additional terms                              | Governed by the .NET Runtime project                            |

## Notes

- CliInvoke v1 and v2 shipped dedicated builder classes (`ArgumentsBuilder`, `EnvironmentVariablesBuilder` etc); v3+ replaces them with `ProcessConfiguration` Spec types (`ArgumentsSpec`, `EnvironmentVariablesSpec` etc). CliWrap provides dedicated builder classes plus a fluent `Command` chain via `Cli.Wrap(...)`.
- CliWrap's repository also contains an informal Terms of Use document, separate from the
  MIT license; the project's stated position is that this is governance signalling rather
  than a binding license addendum. Fork maintainers should read both the MIT license and
  the Terms of Use document before redistributing.
