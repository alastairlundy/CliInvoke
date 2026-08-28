---
title: Extensions — CachingFilePathResolverOptions
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add the options POCO for the cached file-path resolver, with bounded, overridable defaults (SizeLimit 512, 5-minute absolute expiration) plus the D009 invalidation hook.

## What to build

`CachingFilePathResolverOptions` (sealed) with `int SizeLimit` (default 512), `TimeSpan AbsoluteExpirationRelativeToNow` (default 5 minutes), `PostEvictionCallback? PostEvictionCallback`, and `static CachingFilePathResolverOptions Default { get; }`, following `PowerShellMiddlewareOptions` pattern.

## Size

- **Files**: 1 (new)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add CachingFilePathResolverOptions

Where: src/CliInvoke.Extensions/Caching/CachingFilePathResolverOptions.cs (new)

- sealed class; SizeLimit=512, AbsoluteExpirationRelativeToNow=5min, PostEvictionCallback?; static Default.

Verify: Matches PowerShellMiddlewareOptions shape; defaults match D013/D015.

## Context pointers

##### Files

- src/CliInvoke.Specializations/Middleware/PowerShellMiddlewareOptions.cs — options POCO pattern.

##### ADRs

- D013 — default SizeLimit 512. D015 — default absolute 5 min. D009 — PostEvictionCallback invalidation hook.

##### Domain terms

- IMemoryCache — the shared cache; SizeLimit applies to MemoryCacheOptions (D013), not per-resolver.

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T008 — CachingFilePathResolverOptions POCO.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D009 — PostEvictionCallback hook.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D013 — SizeLimit default 512.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D015 — absolute 5 min default.

## Acceptance criteria

- [ ] `CachingFilePathResolverOptions` has `SizeLimit` (default 512), `AbsoluteExpirationRelativeToNow` (default 5 min), `PostEvictionCallback?`, and `static Default`.
- [ ] Shapes follow `PowerShellMiddlewareOptions`.

## Dependencies

Blocked by: None - can start immediately
