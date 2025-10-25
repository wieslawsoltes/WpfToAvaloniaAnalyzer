using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.CodeFixes;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class PropertyMetadataCodeFixTests
{
    [Fact]
    public async Task ConvertPropertyMetadataWithCallback_ToAvaloniaStyledPropertyWithNotify()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty CountProperty =
            DependencyProperty.Register(
                nameof(Count),
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
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<MyControl, int>(nameof(Count), 0, notify: (sender, before) => { if (!before) OnCountChanged(sender, default(DependencyPropertyChangedEventArgs)); });

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
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA005_ConvertPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyMetadataAnalyzer, PropertyMetadataCodeFixProvider>(test, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertPropertyMetadataWithCallbackAndDefaultValue_ToAvaloniaStyledProperty()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty ScoreProperty =
            DependencyProperty.Register(
                nameof(Score),
                typeof(double),
                typeof(MyControl),
                {|#0:new PropertyMetadata(100.0, OnScoreChanged)|});

        public double Score
        {
            get => (double)GetValue(ScoreProperty);
            set => SetValue(ScoreProperty, value);
        }

        private static void OnScoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyControl control)
            {
                // Handle score changed
            }
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly StyledProperty<double> ScoreProperty = AvaloniaProperty.Register<MyControl, double>(nameof(Score), 100.0, notify: (sender, before) => { if (!before) OnScoreChanged(sender, default(DependencyPropertyChangedEventArgs)); });

        public double Score
        {
            get => (double)GetValue(ScoreProperty);
            set => SetValue(ScoreProperty, value);
        }

        private static void OnScoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MyControl control)
            {
                // Handle score changed
            }
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA005_ConvertPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyMetadataAnalyzer, PropertyMetadataCodeFixProvider>(test, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertPropertyMetadataWithStringType_ToAvaloniaStyledProperty()
    {
        var test = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register(
                nameof(Name),
                typeof(string),
                typeof(MyControl),
                {|#0:new PropertyMetadata(""Default"", OnNameChanged)|});

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle name changed
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly StyledProperty<string> NameProperty = AvaloniaProperty.Register<MyControl, string>(nameof(Name), ""Default"", notify: (sender, before) => { if (!before) OnNameChanged(sender, default(DependencyPropertyChangedEventArgs)); });

        public string Name
        {
            get => (string)GetValue(NameProperty);
            set => SetValue(NameProperty, value);
        }

        private static void OnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Handle name changed
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA005_ConvertPropertyMetadata)
            .WithLocation(0);

        await CodeFixTestHelper.VerifyCodeFixAsync<PropertyMetadataAnalyzer, PropertyMetadataCodeFixProvider>(test, expected, fixedCode);
    }
}
