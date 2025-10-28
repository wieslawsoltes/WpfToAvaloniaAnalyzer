using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DependencyPropertyAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

        // Check if the field is static and readonly
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return;
        }

        // Get the type of the field
        var variableDeclaration = fieldDeclaration.Declaration;
        var typeSyntax = variableDeclaration.Type;

        var typeInfo = context.SemanticModel.GetTypeInfo(typeSyntax, context.CancellationToken);
        if (typeInfo.Type == null)
            return;

        // Check if it's a DependencyProperty
        var typeName = typeInfo.Type.Name;
        var typeNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

        if (typeName == "DependencyProperty" && typeNamespace == "System.Windows")
        {
            // Check each variable declared
            foreach (var variable in variableDeclaration.Variables)
            {
                // Check if the field name follows the DependencyProperty naming convention
                var fieldName = variable.Identifier.Text;
                if (fieldName.EndsWith("Property"))
                {
                    // Check if there's an initializer with DependencyProperty.Register
                    if (variable.Initializer?.Value is InvocationExpressionSyntax invocation)
                    {
                        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
                        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                        {
                            if (methodSymbol.Name == "Register" &&
                                methodSymbol.ContainingType?.Name == "DependencyProperty" &&
                                methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows")
                            {
                                var diagnostic = Diagnostic.Create(
                                    DiagnosticDescriptors.WA001_ConvertDependencyPropertyToAvaloniaProperty,
                                    variable.Identifier.GetLocation(),
                                    fieldName);

                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }
    }
}
