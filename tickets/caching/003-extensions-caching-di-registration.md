---
title: Extensions — Caching DI registration and package references
classification: Independent
blocked_by: [002-extensions-caching-resolver, 001-extensions-caching-options]
parent: IMPLEMENTATION-CliInvoke-middleware.md
---

## Goal

Wire the cached resolver via DI and add the `Microsoft.Extensions.Caching.Memory` package to `CliInvoke.Extensions` only (Core stays free of caching, per D010).

## What to build

1. `CachingFilePathResolverExtensions` with `UseCachingFilePathResolver()` and an overload accepting `Action<CachingFilePathResolverOptions>` on `IServiceCollection`. It (a) ensures `IMemoryCache` is registered as Singleton with `MemoryCacheOptions.SizeLimit` from options (default 512); (b) decorates the currently-registered `IFilePathResolver` by capturing its implementation as the inner resolver and re-registering `IFilePathResolver` as `CachingFilePathResolver` (injecting inner, Singleton IMemoryCache, options) at the global lifetime (default Scoped per DD5). Avoid a circular `IFilePathResolver` dependency by resolving the inner from the prior registration, not from `IFilePathResolver` directly.
2. Add `Microsoft.Extensions.Caching.Memory` `PackageVersion` to `src/Directory.Packages.props` (Central Package Management).
3. Add `<PackageReference Include="Microsoft.Extensions.Caching.Memory" />` (no Version) to `src/CliInvoke.Extensions/CliInvoke.Extensions.csproj`.

## Size

- **Files**: 3 (1 new: CachingFilePathResolverExtensions.cs; 2 edits: Directory.Packages.props, CliInvoke.Extensions.csproj)
- **Large Files to be created**: omit
- **Large Edits required**: omit

## Recommended Workflow

### Step 1 - Add UseCachingFilePathResolver extension

Where: src/CliInvoke.Extensions/Caching/CachingFilePathResolverExtensions.cs (new)

- static class; UseCachingFilePathResolver() + configure overload; register IMemoryCache Singleton (SizeLimit from options); decorate IFilePathResolver as CachingFilePathResolver at global lifetime.

Verify: Resolver wrapper is per-scope; cache shared (Singleton). No circular IFilePathResolver.

### Step 2 - Add package to CPM

Where: src/Directory.Packages.props

- Add `<PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="<current>" />`.

Verify: Version present; matches other Microsoft.Extensions.* entries.

### Step 3 - Reference package in Extensions

Where: src/CliInvoke.Extensions/CliInvoke.Extensions.csproj

- Add `<PackageReference Include="Microsoft.Extensions.Caching.Memory" />` (no Version).

Verify: CliInvoke.Extensions builds and can use IMemoryCache.

## Context pointers

##### Files

- src/CliInvoke.Extensions/DependencyInjection/FilePathResolverRegistration.cs — UseCustomFilePathResolver pattern to mirror for decorator swap.
- src/CliInvoke.Extensions/Caching/CachingFilePathResolver.cs — resolver class (TK008).
- src/CliInvoke.Extensions/Caching/CachingFilePathResolverOptions.cs — options (TK007).
- src/Directory.Packages.props — CPM entries.
- src/CliInvoke.Extensions/CliInvoke.Extensions.csproj — package references.
- GLOSSARY.md — DD5 (resolver global lifetime default Scoped).

##### ADRs

- D008 — resolver global lifetime, IMemoryCache Singleton. D009 — cache key/value/TTL. D010 — Extensions placement, Core free of caching. D013 — SizeLimit 512.

##### Domain terms

- IFilePathResolver — decorated; inner captured from prior registration to avoid circular dependency.
- IMemoryCache — shared Singleton cache store.
- Global lifetime (DD5) — resolver follows it (default Scoped), not Singleton.

##### Ledger records

- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T007 — caching DI wiring.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#T009 — UseCachingFilePathResolver() on IServiceCollection; lifetime; decorator swap.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D008 — resolver global lifetime, IMemoryCache Singleton.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D009 — cache key/value/TTL.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D010 — Extensions placement, Core free of caching.
- DECISIONS-CliInvoke-middleware-truncation-caching-retry.md#D013 — SizeLimit 512.

## Acceptance criteria

- [ ] `UseCachingFilePathResolver()` (and configure overload) exists on `IServiceCollection`.
- [ ] `IMemoryCache` registered as Singleton with `MemoryCacheOptions.SizeLimit` from options (default 512).
- [ ] Existing `IFilePathResolver` is decorated as `CachingFilePathResolver` at the global lifetime (default Scoped); inner resolved from prior registration (no circular dependency).
- [ ] `Microsoft.Extensions.Caching.Memory` added to Directory.Packages.props and CliInvoke.Extensions.csproj; CliInvoke.Core takes no caching dependency.

## Dependencies

Blocked by: 002-extensions-caching-resolver, 001-extensions-caching-options
