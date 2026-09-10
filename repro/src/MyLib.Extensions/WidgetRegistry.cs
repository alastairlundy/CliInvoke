using System.Collections.Generic;
using System.Linq;

namespace MyLib.Extensions;

/// <summary>
/// A registry for looking up widgets by name.
/// </summary>
public class WidgetRegistry
{
    private readonly Dictionary<string, Widget> _widgets = new();

    /// <summary>
    /// Registers a widget.
    /// </summary>
    /// <param name="widget">The widget to register.</param>
    public void Register(Widget widget) => _widgets[widget.Name] = widget;

    /// <summary>
    /// Tries to find a widget by name.
    /// </summary>
    /// <param name="name">The widget name.</param>
    /// <param name="widget">The found widget, or <c>null</c>.</param>
    /// <returns><c>true</c> if found.</returns>
    public bool TryGet(string name, out Widget? widget) =>
        _widgets.TryGetValue(name, out widget);

    /// <summary>
    /// Returns all registered widgets.
    /// </summary>
    /// <returns>An array of widgets.</returns>
    public Widget[] GetAll() => _widgets.Values.ToArray();
}
