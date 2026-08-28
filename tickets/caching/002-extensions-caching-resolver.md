---
title: Extensions — CachingFilePathResolver
classification: Independent
blocked_by: [001-extensions-caching-options]
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Add the `CachingFilePathResolver` decorator that caches resolved absolute paths keyed on the raw target, delegating to the inner resolver (PATH-first per GLOSSARY DD1) on a miss.

## What to build

`CachingFilePathResolver : IFilePathResolver` (public sealed). Ctor takes an inner `IFilePathResolver` and `IMemoryCache` (Singleton). Both `ResolveFilePath` and `TryResolveFilePath` check the cache keyed on the raw target; on a miss, delegate to the inner resolver, cache the resolved absolute `FileInfo` with `AbsoluteExpirationRelativeToNow` (from options) and `PostEvictionCallback`, then return.

## Size

- **Files**: 1 (new)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add CachingFilePathResolver

Where: src/CliInvoke.Extensions/Caching/CachingFilePathResolver.cs (new)

- public sealed : IFilePathResolver; ctor(inner, cache, options); both methods check cache by raw target, delegate to inner on miss, cache result with TTL + eviction callback.

Verify: Cache hit returns cached FileInfo; miss delegates to inner (PATH-first); TTL + eviction applied.

## Context pointers

##### Files

- src/CliInvoke.Core/IFilePathResolver.cs — interface implemented.
- src/CliInvoke.Core/FilePathResolverBase.cs — inner resolver; PATH-first order (DD1) preserved on miss.
- src/CliInvoke.Extensions/Caching/CachingFilePathResolverOptions.cs — options (TK007) for TTL/SizeLimit/eviction.

##### ADRs

- D009 — key on raw target, cache resolved absolute path, TTL + eviction. D010 — lives in Extensions, Core free of caching.

##### Domain terms

- IFilePathResolver — the decorated contract; both methods delegate on miss.
- PATH-first resolution (GLOSSARY DD1) — preserved on cache miss.

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T007 — CachingFilePathResolver decorator shape.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D009 — raw-target key, absolute-path value, TTL + eviction.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D010 — Extensions placement, Core free of caching.

## Acceptance criteria

- [ ] `CachingFilePathResolver : IFilePathResolver` with ctor(inner IFilePathResolver, IMemoryCache, options).
- [ ] Both resolve methods cache by raw target; miss delegates to inner (PATH-first per DD1); hit returns cached absolute FileInfo.
- [ ] Cached entry uses AbsoluteExpirationRelativeToNow + PostEvictionCallback from options.

## Dependencies

Blocked by: 001-extensions-caching-options
