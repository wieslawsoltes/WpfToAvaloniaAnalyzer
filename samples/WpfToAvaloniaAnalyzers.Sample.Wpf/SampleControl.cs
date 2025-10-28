using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf;

public class SampleControl : Control
{
    // Example 1: Simple DependencyProperty
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(SampleControl),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    // Example 2: DependencyProperty with default value
    public static readonly DependencyProperty WidthValueProperty =
        DependencyProperty.Register(
            nameof(WidthValue),
            typeof(double),
            typeof(SampleControl),
            new PropertyMetadata(100.0));

    public double WidthValue
    {
        get => (double)GetValue(WidthValueProperty);
        set => SetValue(WidthValueProperty, value);
    }

    // Example 3: Boolean DependencyProperty
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(SampleControl),
            new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    // Example 4: DependencyProperty with property changed callback
    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(
            nameof(Count),
            typeof(int),
            typeof(SampleControl),
            new PropertyMetadata(0, OnCountChanged));

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SampleControl control)
        {
            // Handle property changed
        }
    }
}
