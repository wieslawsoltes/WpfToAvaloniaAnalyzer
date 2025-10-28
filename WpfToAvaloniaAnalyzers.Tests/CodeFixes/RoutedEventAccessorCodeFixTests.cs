using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventAccessorCodeFixTests
{
    [Fact]
    public async Task ConvertsRoutedEventAccessors()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));

        public event RoutedEventHandler {|#0:Click|}
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        private void OnClick(FrameworkElement sender, RoutedEventArgs e) { }

        protected virtual void OnClickCore(RoutedEventArgs e) { }
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
            ""Click"",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(MyControl));

        public event global::System.EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        private void OnClick(global::Avalonia.Interactivity.Interactive sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }

        protected virtual void OnClickCore(global::Avalonia.Interactivity.RoutedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors)
            .WithLocation(0)
            .WithArguments("Click");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventAccessorAnalyzer, RoutedEventAccessorCodeFixProvider>(
            source,
            diagnostic,
            expected,
            numberOfIterations: 1);
    }

    [Fact]
    public async Task ConvertsSpecializedHandlerEventArgs()
    {
        var source = @"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent MouseDownEvent = EventManager.RegisterRoutedEvent(
            ""MouseDown"",
            RoutingStrategy.Bubble,
            typeof(MouseButtonEventHandler),
            typeof(MyControl));

        public event MouseButtonEventHandler {|#0:MouseDown|}
        {
            add => AddHandler(MouseDownEvent, value);
            remove => RemoveHandler(MouseDownEvent, value);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e) { }
    }
}";

        var expected = @"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly RoutedEvent MouseDownEvent = EventManager.RegisterRoutedEvent(
            ""MouseDown"",
            RoutingStrategy.Bubble,
            typeof(MouseButtonEventHandler),
            typeof(MyControl));

        public event global::System.EventHandler<global::Avalonia.Input.PointerPressedEventArgs> MouseDown
        {
            add => AddHandler(MouseDownEvent, value);
            remove => RemoveHandler(MouseDownEvent, value);
        }

        private void OnMouseDown(object sender, global::Avalonia.Input.PointerPressedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA016_ConvertRoutedEventAccessors)
            .WithLocation(0)
            .WithArguments("MouseDown");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventAccessorAnalyzer, RoutedEventAccessorCodeFixProvider>(
            source,
            diagnostic,
            expected,
            numberOfIterations: 1);
    }
}
