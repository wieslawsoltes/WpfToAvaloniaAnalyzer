using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class RoutedEventInstanceHandlerCodeFixTests
{
    [Fact]
    public async Task ConvertsAddHandlerInvocation()
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

        public MyControl()
        {
            AddHandler({|#0:ClickEvent|}, new RoutedEventHandler(OnClick));
        }

        private void OnClick(FrameworkElement sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
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

        public MyControl()
        {
            AddHandler(ClickEvent, OnClick);
        }

        private void OnClick(global::Avalonia.Interactivity.Interactive sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA018_ConvertAddRemoveHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventInstanceHandlerAnalyzer, RoutedEventInstanceHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            numberOfIterations: 1);
    }

    [Fact]
    public async Task ConvertsAddHandlerInvocationWithHandledEventsToo()
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

        public MyControl()
        {
            AddHandler({|#0:ClickEvent|}, new RoutedEventHandler(OnClick), true);
        }

        private void OnClick(object sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
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

        public MyControl()
        {
            AddHandler(ClickEvent, OnClick, handledEventsToo: true);
        }

        private void OnClick(object sender, global::Avalonia.Interactivity.RoutedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA018_ConvertAddRemoveHandler)
            .WithLocation(0)
            .WithArguments("ClickEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventInstanceHandlerAnalyzer, RoutedEventInstanceHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            numberOfIterations: 1);
    }

    [Fact]
    public async Task ConvertsAddHandlerInvocationWithSpecializedArgs()
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

        public MyControl()
        {
            AddHandler({|#0:MouseDownEvent|}, new MouseButtonEventHandler(OnMouseDown));
        }

        private void OnMouseDown(object sender, global::Avalonia.Input.PointerPressedEventArgs e) { }
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

        public MyControl()
        {
            AddHandler(MouseDownEvent, OnMouseDown);
        }

        private void OnMouseDown(object sender, global::Avalonia.Input.PointerPressedEventArgs e) { }
    }
}";

        var diagnostic = new DiagnosticResult(DiagnosticDescriptors.WA018_ConvertAddRemoveHandler)
            .WithLocation(0)
            .WithArguments("MouseDownEvent");

        await CodeFixTestHelper.VerifyCodeFixAsync<RoutedEventInstanceHandlerAnalyzer, RoutedEventInstanceHandlerCodeFixProvider>(
            source,
            diagnostic,
            expected,
            compilerDiagnostics: CompilerDiagnostics.None,
            numberOfIterations: 1);
    }
}
