using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttachedPropertyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA012_ConvertAttachedDependencyProperty);

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

        if (fieldDeclaration.Declaration.Type is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != "DependencyProperty")
        {
            return;
        }

        var semanticModel = context.SemanticModel;

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation)
                continue;

            if (!IsRegisterAttachedInvocation(invocation, semanticModel, context.CancellationToken))
                continue;

            var fieldName = variable.Identifier.Text;
            if (!fieldName.EndsWith("Property"))
                continue;

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.WA012_ConvertAttachedDependencyProperty,
                variable.Identifier.GetLocation(),
                fieldName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool IsRegisterAttachedInvocation(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        return methodSymbol.Name == "RegisterAttached" &&
               methodSymbol.ContainingType?.Name == "DependencyProperty" &&
               methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }
}
