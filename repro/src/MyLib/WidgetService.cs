namespace MyLib;

/// <summary>
/// A service that manages widgets.
/// </summary>
public class WidgetService
{
    /// <summary>
    /// Creates a new widget with the given name.
    /// </summary>
    /// <param name="name">The widget name.</param>
    /// <returns>The created widget.</returns>
    public Widget Create(string name) => new(name);
}

/// <summary>
/// Represents a widget.
/// </summary>
public class Widget
{
    /// <summary>
    /// Gets the widget name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new <see cref="Widget"/>.
    /// </summary>
    public Widget(string name) => Name = name;
}
