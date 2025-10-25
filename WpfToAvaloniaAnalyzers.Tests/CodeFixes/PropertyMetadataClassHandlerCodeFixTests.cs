using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class PropertyMetadataClassHandlerCodeFixTests
{
    [Fact]
    public async Task ConvertsPropertyMetadataCallback()
    {
        var testCode = @"using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                ""Count"",
                typeof(int),
                typeof(MyControl),
                {|#0:new PropertyMetadata(0, OnCountChanged)|});

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        private static void OnCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle property changed
        }
    }
}
";

        var fixedCode = @"using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        static MyControl()
        {
            CountProperty.Changed.AddClassHandler<MyControl>((sender, args) => OnCountChanged(sender, args));
        }

        public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<MyControl, int>(""Count"", 0);

        public int Count
        {
            get => (int)GetValue(CountProperty);
            set => SetValue(CountProperty, value);
        }

        private static void OnCountChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            // Handle property changed
        }
    }
}
";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA008_ConvertPropertyMetadataCallbackToClassHandler)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyMetadataClassHandlerAnalyzer, PropertyMetadataClassHandlerCodeFixProvider>(
            testCode,
            expected,
            fixedCode);
    }
}
