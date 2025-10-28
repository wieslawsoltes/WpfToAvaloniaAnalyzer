using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RoutedEventInstanceHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA018_ConvertAddRemoveHandler);

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

        if (invocation.FirstAncestorOrSelf<EventDeclarationSyntax>() != null)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!IsWpfHandlerMethod(methodSymbol))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;

        var routedEventSymbol = context.SemanticModel.GetSymbolInfo(invocation.ArgumentList.Arguments[0].Expression, context.CancellationToken).Symbol;
        if (routedEventSymbol is not IFieldSymbol fieldSymbol)
            return;

        if (!RoutedEventHelper.IsWpfRoutedEvent(fieldSymbol.Type))
            return;

        if (invocation.ArgumentList.Arguments.Count < 2)
            return;

        var handlerExpression = invocation.ArgumentList.Arguments[1].Expression;
        if (handlerExpression is not ObjectCreationExpressionSyntax)
            return;

        var location = invocation.ArgumentList.Arguments[0].Expression.GetLocation();

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA018_ConvertAddRemoveHandler,
            location,
            fieldSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsWpfHandlerMethod(IMethodSymbol methodSymbol)
    {
        if (!string.Equals(methodSymbol.Name, "AddHandler", StringComparison.Ordinal) &&
            !string.Equals(methodSymbol.Name, "RemoveHandler", StringComparison.Ordinal))
        {
            return false;
        }

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        var namespaceName = containingType.ContainingNamespace?.ToDisplayString();
        return namespaceName == "System.Windows" || namespaceName == "System.Windows.Controls";
    }
}
