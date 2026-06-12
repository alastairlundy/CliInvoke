---
title: Getting Started - Quickstart
layout: simple
---

## Quickstart

Install the package via NuGet:

```xml
<PackageReference Include="CliInvoke" Version="1.0.0" />
```

Add services:

```csharp
var services = new ServiceCollection();
services.AddCliInvoke();
var provider = services.BuildServiceProvider();
```

Run a simple command using a ProcessConfiguration example (see full Getting Started for DI and advanced options).

(Quickstart migrated from existing docs.)
