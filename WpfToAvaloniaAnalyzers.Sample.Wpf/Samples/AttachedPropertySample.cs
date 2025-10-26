using System;
using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Exercises WA012 by defining a WPF attached property with validation and a change callback.
/// </summary>
public static class AttachedPropertySample
{
    public static readonly DependencyProperty DockModeProperty =
        DependencyProperty.RegisterAttached(
            "DockMode",
            typeof(Dock),
            typeof(AttachedPropertySample),
            new FrameworkPropertyMetadata(Dock.Left, OnDockModeChanged),
            IsValidDockMode);

    public static Dock GetDockMode(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (Dock)element.GetValue(DockModeProperty); // WA004_RemoveCastsFromGetValue
    }

    public static void SetDockMode(UIElement element, Dock value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(DockModeProperty, value);
    }

    private static void OnDockModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element && e.NewValue is Dock)
        {
            element.InvalidateArrange();
        }
    }

    private static bool IsValidDockMode(object value) => value is Dock;
}
