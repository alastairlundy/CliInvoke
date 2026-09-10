---
title: Registry
---

# Widget Registry

`WidgetRegistry` lets you look up widgets by name.

```csharp
using MyLib;
using MyLib.Extensions;

var service = new WidgetService();
var registry = new WidgetRegistry();

registry.Register(service.Create("alpha"));
registry.Register(service.Create("beta"));

if (registry.TryGet("alpha", out var widget))
    Console.WriteLine(widget.ToDisplayString());
```
