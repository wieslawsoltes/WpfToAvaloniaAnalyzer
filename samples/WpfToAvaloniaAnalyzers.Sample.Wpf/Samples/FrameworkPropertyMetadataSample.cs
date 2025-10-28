using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Demonstrates WA013 translation of FrameworkPropertyMetadata and the associated class handler conversion (WA008).
/// </summary>
public class FrameworkPropertyMetadataSampleControl : Control
{
    public static readonly DependencyProperty LayoutModeProperty =
        DependencyProperty.Register(
            nameof(LayoutMode),
            typeof(int),
            typeof(FrameworkPropertyMetadataSampleControl),
            new FrameworkPropertyMetadata(
                0,
                FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.Inherits,
                OnLayoutModeChanged),
            ValidateLayoutMode);

    public int LayoutMode
    {
        get => (int)GetValue(LayoutModeProperty); // WA004_RemoveCastsFromGetValue
        set => SetValue(LayoutModeProperty, value);
    }

    private static void OnLayoutModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkPropertyMetadataSampleControl control && e.NewValue is int mode)
        {
            control.SetValue(LayoutModeProperty, mode);
        }
    }

    private static bool ValidateLayoutMode(object value) => value is int number && number >= 0;
}
