using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventFieldCodeFixTests
{
    [Fact]
    public async Task ConvertsRoutedEventField()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent {|#0:ClickEvent|} = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly global::Avalonia.Interactivity.RoutedEvent<global::Avalonia.Interactivity.RoutedEventArgs> ClickEvent = global::Avalonia.Interactivity.RoutedEvent.Register<MyControl, global::Avalonia.Interactivity.RoutedEventArgs>(""Click"", global::Avalonia.Interactivity.RoutingStrategies.Bubble);
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA015_ConvertRoutedEventField)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventFieldAnalyzer, RoutedEventFieldCodeFixProvider>(source, diagnostic, expected);
    }

    [Fact]
    public async Task ConvertsRoutedEventFieldAlongsideDependencyProperty()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            ""Title"",
            typeof(string),
            typeof(MyControl));

        public static readonly RoutedEvent {|#0:FooEvent|} = EventManager.RegisterRoutedEvent(
            ""Foo"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            ""Title"",
            typeof(string),
            typeof(MyControl));

        public static readonly global::Avalonia.Interactivity.RoutedEvent<global::Avalonia.Interactivity.RoutedEventArgs> FooEvent = global::Avalonia.Interactivity.RoutedEvent.Register<MyControl, global::Avalonia.Interactivity.RoutedEventArgs>(""Foo"", global::Avalonia.Interactivity.RoutingStrategies.Bubble);
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA015_ConvertRoutedEventField)
            .WithLocation(0)
            .WithArguments("FooEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventFieldAnalyzer, RoutedEventFieldCodeFixProvider>(source, diagnostic, expected);
    }
}
