using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertyMetadataClassHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA008_ConvertPropertyMetadataCallbackToClassHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        if (objectCreation.ArgumentList == null || objectCreation.ArgumentList.Arguments.Count < 2)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type == null)
            return;

        if (!IsWpfPropertyMetadata(typeInfo.Type))
            return;

        if (!ContainsCallback(objectCreation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.WA008_ConvertPropertyMetadataCallbackToClassHandler,
            objectCreation.GetLocation()));
    }

    private static bool IsWpfPropertyMetadata(ITypeSymbol typeSymbol)
    {
        var current = typeSymbol;
        while (current != null)
        {
            if (current.Name == "PropertyMetadata" && current.ContainingNamespace?.ToDisplayString() == "System.Windows")
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static bool ContainsCallback(ObjectCreationExpressionSyntax objectCreation)
    {
        return objectCreation.ArgumentList?.Arguments.Count >= 2 &&
               objectCreation.ArgumentList.Arguments[1].Expression is not null;
    }
}
