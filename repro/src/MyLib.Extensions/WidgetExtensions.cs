using System;

namespace MyLib.Extensions;

/// <summary>
/// Extension methods for <see cref="Widget"/>.
/// </summary>
public static class WidgetExtensions
{
    /// <summary>
    /// Returns a display-friendly string for the widget.
    /// </summary>
    /// <param name="widget">The widget.</param>
    /// <returns>A formatted string.</returns>
    public static string ToDisplayString(this Widget widget) => $"Widget: {widget.Name}";

    /// <summary>
    /// Checks whether the widget name matches the given filter.
    /// </summary>
    /// <param name="widget">The widget.</param>
    /// <param name="filter">The filter substring.</param>
    /// <returns><c>true</c> if the name contains the filter.</returns>
    public static bool Matches(this Widget widget, string filter) =>
        widget.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
}
