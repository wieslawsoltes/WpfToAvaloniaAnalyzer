using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using WpfToAvaloniaAnalyzers;

namespace WpfToAvaloniaAnalyzers.Tests.RoutedEvents;

public class RoutedEventHelperTests
{
    [Fact]
    public void GetAvaloniaEventArgsType_MapsMouseEventArgs()
    {
        const string code = @"
namespace System.Windows.Input
{
    public class MouseEventArgs { }

    public delegate void MouseEventHandler(object sender, MouseEventArgs e);
}";

        var compilation = CreateCompilation(code);
        var handlerSymbol = compilation.GetTypeByMetadataName("System.Windows.Input.MouseEventHandler");
        Assert.NotNull(handlerSymbol);

        var mapped = RoutedEventHelper.GetAvaloniaEventArgsType(handlerSymbol, out var originalEventArgsSymbol);

        Assert.NotNull(originalEventArgsSymbol);
        Assert.Equal("System.Windows.Input.MouseEventArgs", originalEventArgsSymbol!.ToDisplayString());
        Assert.Equal("global::Avalonia.Input.PointerEventArgs", mapped.ToString());
    }

    [Fact]
    public void GetAvaloniaEventArgsType_UnknownDefaultsToRoutedEventArgs()
    {
        const string code = @"
namespace TestNamespace
{
    public class CustomEventArgs { }

    public delegate void CustomHandler(object sender, CustomEventArgs e);
}";

        var compilation = CreateCompilation(code);
        var handlerSymbol = compilation.GetTypeByMetadataName("TestNamespace.CustomHandler");
        Assert.NotNull(handlerSymbol);

        var mapped = RoutedEventHelper.GetAvaloniaEventArgsType(handlerSymbol, out var originalEventArgsSymbol);

        Assert.NotNull(originalEventArgsSymbol);
        Assert.Equal("TestNamespace.CustomEventArgs", originalEventArgsSymbol!.ToDisplayString());
        Assert.Equal("global::Avalonia.Interactivity.RoutedEventArgs", mapped.ToString());
    }

    [Fact]
    public void ConvertRoutingStrategyExpression_RewritesIdentifiers()
    {
        var expression = SyntaxFactory.ParseExpression("RoutingStrategy.Bubble | RoutingStrategy.Direct");
        var converted = RoutedEventHelper.ConvertRoutingStrategyExpression(expression);

        Assert.Equal("global::Avalonia.Interactivity.RoutingStrategies.Bubble | global::Avalonia.Interactivity.RoutingStrategies.Direct", converted.ToString());
    }

    [Fact]
    public void ConvertRoutingStrategyExpression_HandlesFullyQualifiedNames()
    {
        var expression = SyntaxFactory.ParseExpression("System.Windows.RoutingStrategy.Tunnel");
        var converted = RoutedEventHelper.ConvertRoutingStrategyExpression(expression);

        Assert.Equal("global::Avalonia.Interactivity.RoutingStrategies.Tunnel", converted.ToString());
    }

    [Fact]
    public void ExtractType_ReturnsInnerType()
    {
        var expression = SyntaxFactory.ParseExpression("typeof(MyControl)");
        var typeSyntax = RoutedEventHelper.ExtractType(expression);

        Assert.Equal("MyControl", typeSyntax?.ToString());
    }

    [Fact]
    public void GuessOwnerType_ReturnsContainingClassName()
    {
        const string code = @"
class OwnerControl
{
    public static readonly System.Windows.RoutedEvent FooEvent;
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var field = syntaxTree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();

        var owner = RoutedEventHelper.GuessOwnerType(field);

        Assert.Equal("OwnerControl", owner?.ToString());
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
