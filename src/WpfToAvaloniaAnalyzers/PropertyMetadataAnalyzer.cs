using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyMetadataAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA005_ConvertPropertyMetadata);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

        // Check if this is a PropertyMetadata creation
        if (objectCreation.Type is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != "PropertyMetadata")
        {
            return;
        }

        // Use semantic model to ensure this is System.Windows.PropertyMetadata
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        var typeSymbol = typeInfo.Type;

        if (typeSymbol == null)
            return;

        // Check if this is a WPF PropertyMetadata (System.Windows.PropertyMetadata or derived)
        var isWpfPropertyMetadata = typeSymbol.ToDisplayString().StartsWith("System.Windows.") &&
                                     typeSymbol.Name == "PropertyMetadata";

        if (!isWpfPropertyMetadata)
            return;

        // Check if it has a property changed callback (2nd argument)
        if (objectCreation.ArgumentList?.Arguments.Count >= 2)
        {
            var secondArg = objectCreation.ArgumentList.Arguments[1].Expression;

            // Check if the second argument is a method reference (callback)
            if (secondArg is IdentifierNameSyntax ||
                secondArg is SimpleLambdaExpressionSyntax ||
                secondArg is ParenthesizedLambdaExpressionSyntax)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.WA005_ConvertPropertyMetadata,
                    objectCreation.GetLocation());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
