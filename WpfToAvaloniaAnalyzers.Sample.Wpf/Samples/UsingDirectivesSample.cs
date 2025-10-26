using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

/// <summary>
/// Keeps an explicit set of WPF using directives so WA002 has a canonical reference sample.
/// </summary>
public static class UsingDirectivesSample
{
    public static void EnsureHeight(Control control)
    {
        control ??= new Control();
        control.SetValue(FrameworkElement.HeightProperty, 42.0);
    }
}
