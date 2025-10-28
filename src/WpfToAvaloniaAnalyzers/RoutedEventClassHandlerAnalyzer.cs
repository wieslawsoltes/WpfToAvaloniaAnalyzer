using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RoutedEventClassHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA017_ConvertRegisterClassHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol == null)
            return;

        if (!string.Equals(symbol.Name, "RegisterClassHandler", StringComparison.Ordinal))
            return;

        if (symbol.ContainingType?.Name != "EventManager" ||
            symbol.ContainingType.ContainingNamespace?.ToDisplayString() != "System.Windows")
        {
            return;
        }

        var eventName = GetEventName(context.SemanticModel, invocation.ArgumentList.Arguments, context.CancellationToken);
        var eventArgument = invocation.ArgumentList.Arguments.Count > 1
            ? invocation.ArgumentList.Arguments[1].Expression
            : null;

        var location = eventArgument?.GetLocation() ?? invocation.GetLocation();

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA017_ConvertRegisterClassHandler,
            location,
            eventName ?? symbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static string? GetEventName(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments, System.Threading.CancellationToken cancellationToken)
    {
        if (arguments.Count < 2)
            return null;

        var expression = arguments[1].Expression;
        var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (symbol != null)
            return symbol.Name;

        if (expression is IdentifierNameSyntax identifier)
            return identifier.Identifier.Text;

        return null;
    }
}
