using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RoutedEventAddOwnerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA020_ConvertAddOwner);

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

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!string.Equals(methodSymbol.Name, "AddOwner", StringComparison.Ordinal))
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null || !RoutedEventHelper.IsWpfRoutedEvent(containingType))
            return;

        string? eventName = null;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            eventName = memberAccess.Expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.Text,
                MemberAccessExpressionSyntax nested => nested.Name.Identifier.Text,
                _ => null
            };
        }

        Location location = invocation.Expression is MemberAccessExpressionSyntax eventAccess
            ? eventAccess.Expression.GetLocation()
            : invocation.GetLocation();

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA020_ConvertAddOwner,
            location,
            eventName ?? methodSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }
}
