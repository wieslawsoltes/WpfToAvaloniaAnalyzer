using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WpfControlAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> WpfControlNames = ImmutableHashSet.Create(
        "DockPanel",
        "Button",
        "TextBox",
        "ListBox",
        "ComboBox",
        "StackPanel",
        "Grid",
        "Border",
        "TextBlock",
        "Menu",
        "MenuItem",
        "StatusBar"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WPF001_UseAvaloniaControl);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeIdentifierName, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (ObjectCreationExpressionSyntax)context.Node;
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);

        if (typeInfo.Type == null)
            return;

        var typeName = typeInfo.Type.Name;
        var typeNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();

        // Check if it's a WPF control (System.Windows.Controls namespace)
        if (typeNamespace == "System.Windows.Controls" && WpfControlNames.Contains(typeName))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.WPF001_UseAvaloniaControl,
                objectCreation.Type.GetLocation(),
                typeName);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeIdentifierName(SyntaxNodeAnalysisContext context)
    {
        var identifierName = (IdentifierNameSyntax)context.Node;

        // Skip if this is part of a using directive or namespace
        if (identifierName.Parent is UsingDirectiveSyntax or NamespaceDeclarationSyntax or QualifiedNameSyntax)
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken);

        if (symbolInfo.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        var typeName = typeSymbol.Name;
        var typeNamespace = typeSymbol.ContainingNamespace?.ToDisplayString();

        // Check if it's a WPF control type reference
        if (typeNamespace == "System.Windows.Controls" && WpfControlNames.Contains(typeName))
        {
            // Only report if this is a meaningful usage (not just a type parameter, etc.)
            if (identifierName.Parent is not (TypeArgumentListSyntax or BaseTypeSyntax))
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.WPF001_UseAvaloniaControl,
                    identifierName.GetLocation(),
                    typeName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}