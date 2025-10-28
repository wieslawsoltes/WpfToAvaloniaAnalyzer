using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Triggers WA001, WA003, WA004, WA005, and WA006 by defining classic dependency properties on a WPF Control.
/// </summary>
public class DependencyPropertySampleControl : Control
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(DependencyPropertySampleControl),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty); // WA004_RemoveCastsFromGetValue
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(
            nameof(Count),
            typeof(int),
            typeof(DependencyPropertySampleControl),
            new PropertyMetadata(0));

    public int Count
    {
        get => (int)GetValue(CountProperty); // WA004_RemoveCastsFromGetValue
        set => SetValue(CountProperty, value);
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DependencyPropertySampleControl control && e.NewValue is string text && text.Length > 0)
        {
            control.SetValue(CountProperty, control.Count + 1);
        }
    }
}
