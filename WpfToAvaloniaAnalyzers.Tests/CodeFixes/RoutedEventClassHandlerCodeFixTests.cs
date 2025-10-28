using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventClassHandlerCodeFixTests
{
    [Fact]
    public async Task ConvertsRegisterClassHandler()
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

        static MyControl()
        {
            EventManager.RegisterClassHandler(typeof(MyControl), {|#0:ClickEvent|}, new RoutedEventHandler(OnClick));
        }

        private static void OnClick(FrameworkElement sender, RoutedEventArgs e) { }
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

        static MyControl()
        {
            ClickEvent.AddClassHandler(typeof(MyControl), (sender, args) => OnClick(sender, args));
        }

        private static void OnClick(global::Avalonia.Interactivity.Interactive sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA017_ConvertRegisterClassHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventClassHandlerAnalyzer, RoutedEventClassHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ConvertsRegisterClassHandlerWithHandledEventsToo()
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

        static MyControl()
        {
            EventManager.RegisterClassHandler(typeof(MyControl), {|#0:ClickEvent|}, new RoutedEventHandler(OnClick), true);
        }

        private static void OnClick(FrameworkElement sender, RoutedEventArgs e) { }
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

        static MyControl()
        {
            ClickEvent.AddClassHandler(typeof(MyControl), (sender, args) => OnClick(sender, args), handledEventsToo: true);
        }

        private static void OnClick(global::Avalonia.Interactivity.Interactive sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA017_ConvertRegisterClassHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventClassHandlerAnalyzer, RoutedEventClassHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Fact]
    public async Task ConvertsRegisterClassHandlerWithSpecializedArgs()
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

        static MyControl()
        {
            EventManager.RegisterClassHandler(typeof(MyControl), {|#0:MouseDownEvent|}, new MouseButtonEventHandler(OnMouseDown));
        }

        private static void OnMouseDown(object sender, MouseButtonEventArgs e) { }
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

        static MyControl()
        {
            MouseDownEvent.AddClassHandler(typeof(MyControl), (sender, args) => OnMouseDown(sender, args));
        }

        private static void OnMouseDown(object sender, global::Avalonia.Input.PointerPressedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA017_ConvertRegisterClassHandler)
            .WithLocation(0)
            .WithArguments("MouseDownEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventClassHandlerAnalyzer, RoutedEventClassHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None);
    }
}
