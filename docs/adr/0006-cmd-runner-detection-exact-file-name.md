# ADR 0006: cmd runner detection is exact file-name matching, shared by routing and validation

- Status: accepted
- Date: 2026-11-15

## Context

The runner factory recognises three runner categories (PowerShell, cmd, non-shell) and picks a
delivery shape per category. Recognition is path-based: the runner is identified from
`TargetFilePath` before anything is launched, so no stronger identity is available at
composition time.

Until now the two checks that depended on that recognition disagreed. Routing used a substring
match — the runner path merely *containing* `cmd` selected the cmd composition shape — while the
composition-level validation check matched the runner's file name exactly (`cmd` or `cmd.exe`,
case-insensitive). The consequence: a runner whose path contained `cmd` without its file name
being `cmd`/`cmd.exe` was routed to the cmd composition shape but skipped the runner-side
validation, and nothing bound the two checks together. The divergence was found in review, not
by tests.

Routing decides what the text will experience. The cmd composition delivers a single composed
string precisely because cmd.exe's own `/c` quote-strip and re-parse do not match .NET's
argument-list quoting (rationale in commit `8f8a5b26`; the string/list contract is ADR 0005).
A runner should only be treated as cmd — and only be validated as cmd — when the library can
honestly claim cmd parse semantics for it, and the file name is the identifier it actually has.

## Decision

1. cmd runner detection is **exact file-name matching**: `Path.GetFileName` equals `cmd` or
   `cmd.exe`, case-insensitive. The predicate lives in one place
   (`CmdCompositionArgumentValidator.IsCmdRunner`), and both the composition routing and the
   factory-level validation hook call it. They cannot drift apart again.
2. **The `UseCmd` middleware validates through the same entry point.** Its composition wraps
   caller text the same way the factory's cmd branch does, so it calls the shared
   `ValidateCompositionSource` (target path plus wrapped arguments in the shape the composition
   consumes) rather than relying on the escaper alone. The factory's cmd branch delegates its
   target-path and wrapped-argument checks to the same method. Validation and composition cannot
   be ordered differently in the two places that compose for cmd.
3. **Target-side checks keep exact/extension matching** (`IsCmdOrBatchTarget`, and the 2.x
   launch-time guard). At those points the parser identity is known — the wrapped or launched
   process — so the check is precise. Substring path matching there would reject arguments for
   benign non-cmd targets whose paths pass through a directory containing `cmd`.
4. On the 2.x train there is no cmd-specific routing branch (delivery is uniformly tokenized);
   its hook is semantic and uses the same exact-match predicate. The rejection character set and
   behavior are identical on every train; only the routing context differs.
5. The matching rule and its limit are stated in the XML documentation of `IsCmdRunner`, the
   runner factory, and `CmdMiddleware`.

## Consequences

**Positive**

- The set of compositions validated is exactly the set routed to cmd composition — by
  construction, through a single call site.
- Runners whose paths merely contain `cmd` keep tokenized delivery and their argument values
  keep their meaning for that runner; no over-rejection.
- One name for one idea; the next reader reads intent instead of re-deriving two predicates.

**Negative**

- A genuine cmd.exe binary used under a different file name (a copy, a shim) is not detected: it
  routes to the tokenized non-shell branch, the validation hook does not fire for it, and text
  it receives through argument-list delivery is re-parsed by cmd.exe's own rules. Callers using
  a renamed cmd must either keep metacharacters out of the arguments or compose through
  `CmdProcessConfiguration`, where quoting is the caller's own.
- Path-based shell detection stays heuristic for every shell; this is the same limitation the
  PowerShell detection already carries.

**Mitigations**

- Regression tests assert both sides of the predicate: a `cmd.exe` runner routes to the composed
  shape and its arguments are validated
  (`RunnerConfigurationFactoryArgumentValidationTests`), and a runner whose path contains `cmd`
  but whose file name is neither `cmd` nor `cmd.exe` routes to the tokenized branch and is not
  runner-validated (`RunnerPathContainingCmdWithoutCmdFileName_IsNotTreatedAsCmdRunner`).
- The `UseCmd` middleware path has the same rejection coverage
  (`ShellInjectionSecurityTests`: metacharacters in the target path and in the wrapped arguments
  are refused with the naming exception; a benign argument list still composes with per-entry
  quoting), so the two composition sites are held to one behavior by tests, not by convention.
- The runner-identification contract is published in the XML documentation of the factory, the
  middleware, and the predicate, in the README, and in `site/docs`, so the discouragement of
  renamed shell runners is discoverable rather than a silent behavior cliff.
- This record exists so that broadening the match back to a substring requires a deliberate ADR
  revision, not a drive-by edit.

## Alternatives considered

- **Broaden validation to the routing predicate** (substring `cmd` in the runner path):
  rejected — it rejects metacharacter-bearing arguments for runners that are not cmd and whose
  paths merely contain `cmd`; validation must match the parse semantics actually applied, and
  the substring shape is not one of them.
- **Keep the two predicates as they were**: rejected — coverage and routing silently diverged,
  and only review caught it.
- **Exact-match routing plus removing the cmd composition branch** (tokenized delivery
  everywhere): rejected — the composed shape exists because cmd's `/c` quote-strip defeats
  argument-list quoting for quoted targets; removing it breaks benign quoted targets to cmd
  runners.
- **Detect renamed cmd binaries at launch time** (for example by reading version information
  from the runner file): rejected — it adds a file read and Windows-only behavior to every
  composition, version metadata is neither a reliable identity proof nor guaranteed present,
  and a heuristic the library cannot state honestly is worse than a documented file-name
  contract.
