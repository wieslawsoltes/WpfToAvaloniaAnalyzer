using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WpfToAvaloniaAnalyzers.Sample.Avalonia.Reference;

public static class RoutedEventSamplesConverted
{
    public static readonly RoutedEvent<RoutedEventArgs> FooEvent =
        RoutedEvent.Register<WpfToAvaloniaAnalyzers.Sample.Wpf.Samples.RoutedEventSamples, RoutedEventArgs>(
            "Foo",
            RoutingStrategies.Bubble);

    public static readonly RoutedEvent BarEvent = Button.ClickEvent;
}
