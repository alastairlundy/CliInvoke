---
title: Using Widgets
---

# Using Widgets

Create widgets with `WidgetService` and manipulate them with extension methods.

```csharp
using MyLib;
using MyLib.Extensions;

var service = new WidgetService();
var widget = service.Create("demo");

// Extension method
Console.WriteLine(widget.ToDisplayString());
Console.WriteLine(widget.Matches("dem")); // true
```
