using System.Windows;
using System.Windows.Controls;

namespace WpfToAvaloniaAnalyzers.Sample.Wpf.Samples;

public class RoutedEventSamples : Control
{
    public static readonly RoutedEvent FooEvent =
        EventManager.RegisterRoutedEvent(
            "Foo",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(RoutedEventSamples));

    public static readonly RoutedEvent BarEvent = Button.ClickEvent;

    public static readonly DependencyProperty BazProperty =
        DependencyProperty.Register(
            "Baz",
            typeof(int),
            typeof(RoutedEventSamples),
            new PropertyMetadata(0, OnBazChanged));

    public event RoutedEventHandler Foo
    {
        add => AddHandler(FooEvent, value);
        remove => RemoveHandler(FooEvent, value);
    }

    public int Baz
    {
        get => (int)GetValue(BazProperty);
        set => SetValue(BazProperty, value);
    }

    public RoutedEventSamples()
    {
        AddHandler(BarEvent, OnBarRaised, handledEventsToo: true);
    }

    private static void OnBazChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is RoutedEventSamples samples)
        {
            samples.RaiseEvent(new RoutedEventArgs(FooEvent, samples));
        }
    }

    private void OnBarRaised(object sender, RoutedEventArgs e)
    {
        RemoveHandler(BarEvent, OnBarRaised);
    }
}
