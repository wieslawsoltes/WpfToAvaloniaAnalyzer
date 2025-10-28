using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FrameworkPropertyMetadataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA013_TranslateFrameworkPropertyMetadata);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax objectCreation)
            return;

        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;

        if (typeSymbol == null ||
            typeSymbol.Name != "FrameworkPropertyMetadata" ||
            typeSymbol.ContainingNamespace?.ToDisplayString() != "System.Windows")
        {
            return;
        }

        if (!IsRegisterInvocationArgument(objectCreation, context.SemanticModel, context.CancellationToken))
            return;

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.WA013_TranslateFrameworkPropertyMetadata,
            objectCreation.Type.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsRegisterInvocationArgument(
        ObjectCreationExpressionSyntax metadataCreation,
        SemanticModel semanticModel,
        System.Threading.CancellationToken cancellationToken)
    {
        if (metadataCreation.Parent is not ArgumentSyntax argument ||
            argument.Parent is not ArgumentListSyntax argumentList ||
            argumentList.Parent is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        if (methodSymbol.Name != "Register")
            return false;

        return methodSymbol.ContainingType?.Name == "DependencyProperty" &&
               methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }
}
