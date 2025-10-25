using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommonDependencyPropertyAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AttributeSyntax attributeSyntax)
            return;

        var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol == null)
            return;

        var attributeType = symbol.ContainingType;
        if (attributeType == null)
            return;

        if (!CommonDependencyPropertyAttributeHelper.MatchesAttributeType(attributeType))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA011_RemoveCommonDependencyPropertyAttribute,
            attributeSyntax.GetLocation(),
            attributeSyntax.Name.ToString());

        context.ReportDiagnostic(diagnostic);
    }
}
