using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RoutedEventFieldAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA015_ConvertRoutedEventField);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not FieldDeclarationSyntax fieldDeclaration)
            return;

        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return;
        }

        var declaration = fieldDeclaration.Declaration;
        if (declaration.Type == null)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(declaration.Type, context.CancellationToken);
        if (typeInfo.Type == null)
            return;

        if (!RoutedEventHelper.IsWpfRoutedEvent(typeInfo.Type))
            return;

        foreach (var variable in declaration.Variables)
        {
            if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation)
                continue;

            if (!IsRegisterRoutedEventInvocation(invocation, context.SemanticModel, context.CancellationToken))
                continue;

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.WA015_ConvertRoutedEventField,
                variable.Identifier.GetLocation(),
                variable.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsRegisterRoutedEventInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        if (methodSymbol.Name != "RegisterRoutedEvent")
            return false;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        return containingType.Name == "EventManager" &&
               containingType.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }
}
