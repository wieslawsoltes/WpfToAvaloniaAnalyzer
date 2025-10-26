using System.Windows;
using System.Windows.Controls;
using MS.Internal.PresentationFramework;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Targets WA011 by using the CommonDependencyProperty attribute on a dependency property field.
/// </summary>
public class CommonDependencyPropertySampleControl : Control
{
    [CommonDependencyProperty]
    public static readonly DependencyProperty HighlightBrushProperty =
        DependencyProperty.Register(
            nameof(HighlightBrush),
            typeof(object),
            typeof(CommonDependencyPropertySampleControl),
            new PropertyMetadata(null));

    public object? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }
}
