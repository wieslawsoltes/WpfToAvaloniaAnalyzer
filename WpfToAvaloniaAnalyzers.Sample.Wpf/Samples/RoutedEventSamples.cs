using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

public class RoutedEventSamples : Control
{
    public static readonly global::Avalonia.Interactivity.RoutedEvent<global::Avalonia.Interactivity.RoutedEventArgs> FooEvent = global::Avalonia.Interactivity.RoutedEvent.Register<RoutedEventSamples, global::Avalonia.Interactivity.RoutedEventArgs>("Foo", global::Avalonia.Interactivity.RoutingStrategies.Bubble);

    public static readonly global::Avalonia.Interactivity.RoutedEvent BarEvent = Button.ClickEvent;
}
