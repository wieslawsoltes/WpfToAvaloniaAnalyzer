using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using WpfToAvaloniaAnalyzers.Tests.Helpers;

namespace WpfToAvaloniaAnalyzers.Tests.CodeFixes;

public class DependencyPropertyCodeFixTests
{
    [Fact]
    public async Task ConvertDependencyPropertyToStyledProperty()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:MyPropertyProperty|} =
            DependencyProperty.Register(
                nameof(MyProperty),
                typeof(string),
                typeof(MyControl));

        public string MyProperty
        {
            get => (string)GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        public static readonly StyledProperty<string> MyPropertyProperty = AvaloniaProperty.Register<MyControl, string>(nameof(MyProperty), default(string));

        public string MyProperty
        {
            get => (string)GetValue(MyPropertyProperty);
            set => SetValue(MyPropertyProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("MyPropertyProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertDependencyProperty_WithFrameworkMetadataDefaultValue()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public enum ShirtColors
    {
        None,
        Red
    }

    public class Shirt : Control
    {
        public static readonly DependencyProperty {|#0:ShirtColorProperty|} =
            DependencyProperty.Register(
                nameof(ShirtColor),
                typeof(ShirtColors),
                typeof(Shirt),
                new FrameworkPropertyMetadata(ShirtColors.None));

        public ShirtColors ShirtColor
        {
            get => (ShirtColors)GetValue(ShirtColorProperty);
            set => SetValue(ShirtColorProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public enum ShirtColors
    {
        None,
        Red
    }

    public class Shirt : Avalonia.Controls.Control
    {
        public static readonly StyledProperty<ShirtColors> ShirtColorProperty = AvaloniaProperty.Register<Shirt, ShirtColors>(nameof(ShirtColor), ShirtColors.None);

        public ShirtColors ShirtColor
        {
            get => (ShirtColors)GetValue(ShirtColorProperty);
            set => SetValue(ShirtColorProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("ShirtColorProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertDependencyProperty_WithFrameworkMetadataOptions()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:RenderHintProperty|} =
            DependencyProperty.Register(
                nameof(RenderHint),
                typeof(double),
                typeof(MyControl),
                new FrameworkPropertyMetadata(
                    1.0,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double RenderHint
        {
            get => (double)GetValue(RenderHintProperty);
            set => SetValue(RenderHintProperty, value);
        }
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        static MyControl()
        {
            AffectsRender<MyControl>(RenderHintProperty);
        }

        public static readonly StyledProperty<double> RenderHintProperty = AvaloniaProperty.Register<MyControl, double>(nameof(RenderHint), 1.0);

        public double RenderHint
        {
            get => (double)GetValue(RenderHintProperty);
            set => SetValue(RenderHintProperty, value);
        }
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("RenderHintProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertDependencyProperty_WithMetadataCallbacks()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class Shirt : Control
    {
        public static readonly DependencyProperty {|#0:ButtonColorProperty|} =
            DependencyProperty.Register(
                nameof(ButtonColor),
                typeof(int),
                typeof(Shirt),
                new FrameworkPropertyMetadata(
                    1,
                    OnButtonColorChanged,
                    CoerceButtonColor),
                ValidateButtonColor);

        public int ButtonColor
        {
            get => (int)GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        private static void OnButtonColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(ButtonColorProperty);
        }

        private static object CoerceButtonColor(DependencyObject d, object value)
        {
            return value;
        }

        private static bool ValidateButtonColor(object value) => true;
    }
}";

        var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class Shirt : Avalonia.Controls.Control
    {
        static Shirt()
        {
            ButtonColorProperty.Changed.AddClassHandler<Shirt, int>((Shirt sender, AvaloniaPropertyChangedEventArgs<int> args) => OnButtonColorChanged(sender, args));
        }

        public static readonly StyledProperty<int> ButtonColorProperty = AvaloniaProperty.Register<Shirt, int>(nameof(ButtonColor), 1, validate: ValidateButtonColor, coerce: CoerceButtonColor);

        public int ButtonColor
        {
            get => (int)GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        private static void OnButtonColorChanged(Shirt d, AvaloniaPropertyChangedEventArgs<int> e)
        {
            d.CoerceValue(ButtonColorProperty);
        }

        private static int CoerceButtonColor(AvaloniaObject d, int value)
        {
            return value;
        }

        private static bool ValidateButtonColor(int value) => true;
    }
}";

        var expected = new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
            .WithLocation(0)
            .WithArguments("ButtonColorProperty");

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task ConvertMultipleDependencyProperties()
    {
        var testCode = @"
using System.Windows;
using System.Windows.Controls;

namespace TestNamespace
{
    public class MyControl : Control
    {
        public static readonly DependencyProperty {|#0:TitleProperty|} =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MyControl));

        public string Title { get; set; }

        public static readonly DependencyProperty {|#1:CountProperty|} =
            DependencyProperty.Register(
                nameof(Count),
                typeof(int),
                typeof(MyControl));

        public int Count { get; set; }
    }
}";

var fixedCode = @"
using System.Windows;
using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;

namespace TestNamespace
{
    public class MyControl : Avalonia.Controls.Control
    {
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MyControl, string>(nameof(Title), default(string));

        public string Title { get; set; }

        public static readonly StyledProperty<int> CountProperty = AvaloniaProperty.Register<MyControl, int>(nameof(Count), default(int));

        public int Count { get; set; }
    }
}";

        var expected = new[]
        {
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(0)
                .WithArguments("TitleProperty"),
            new DiagnosticResult(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty)
                .WithLocation(1)
                .WithArguments("CountProperty")
        };

        await CodeFixTestHelper.VerifyCodeFixAsync(testCode, fixedCode, expected);
    }

    [Fact]
    public async Task ConvertDependencyProperty_GeneratesValidAvaloniaCode()
    {
        // This test verifies that properly migrated Avalonia code compiles successfully
        // (after user manually changes base class and removes cast)
        var avaloniaCode = @"
using Avalonia;

namespace TestNamespace
{
    public class MyControl : AvaloniaObject
    {
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<MyControl, string>(nameof(Title), string.Empty);

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
    }
}";

        // Verify the Avalonia code compiles
        var nugetPackagesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        var avaloniaBasePath = Path.Combine(nugetPackagesPath, "avalonia", "11.3.7", "lib", "net8.0");

        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("AvaloniaTestProject", LanguageNames.CSharp);

        // Add .NET 8.0 references
        foreach (var reference in ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).Result)
        {
            project = project.AddMetadataReference(reference);
        }

        // Add Avalonia references
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(avaloniaBasePath, "Avalonia.Base.dll")));
        project = project.AddMetadataReference(MetadataReference.CreateFromFile(Path.Combine(avaloniaBasePath, "Avalonia.Controls.dll")));

        project = project.WithCompilationOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

        var document = project.AddDocument("TestAvalonia.cs", avaloniaCode);
        var compilation = await document.Project.GetCompilationAsync();

        var diagnostics = compilation!.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
        if (diagnostics.Any())
        {
            var errorMessages = string.Join(Environment.NewLine, diagnostics.Select(d => d.ToString()));
            throw new InvalidOperationException($"Avalonia code has compilation errors:{Environment.NewLine}{errorMessages}");
        }
    }
}
