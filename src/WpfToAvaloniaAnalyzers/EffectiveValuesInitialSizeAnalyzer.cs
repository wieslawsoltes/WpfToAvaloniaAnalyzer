using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EffectiveValuesInitialSizeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA014_RemoveEffectiveValuesInitialSize);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not PropertyDeclarationSyntax propertyDeclaration)
            return;

        if (!propertyDeclaration.Identifier.Text.Equals("EffectiveValuesInitialSize", System.StringComparison.Ordinal))
            return;

        if (!propertyDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            return;

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken);
        if (propertySymbol?.IsOverride != true)
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA014_RemoveEffectiveValuesInitialSize,
            propertyDeclaration.Identifier.GetLocation(),
            propertyDeclaration.Identifier.Text);

        context.ReportDiagnostic(diagnostic);
    }
}
