using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyChangedCallbackAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Check if method has exactly 2 parameters
        if (methodDeclaration.ParameterList.Parameters.Count != 2)
            return;

        var firstParam = methodDeclaration.ParameterList.Parameters[0];
        var secondParam = methodDeclaration.ParameterList.Parameters[1];

        // Get semantic information for the parameter types
        var firstParamSymbol = context.SemanticModel.GetDeclaredSymbol(firstParam);
        var secondParamSymbol = context.SemanticModel.GetDeclaredSymbol(secondParam);

        if (firstParamSymbol?.Type == null || secondParamSymbol?.Type == null)
            return;

        var firstParamType = firstParamSymbol.Type.ToDisplayString();
        var secondParamType = secondParamSymbol.Type.ToDisplayString();

        // Check if this matches WPF property changed callback signature:
        // (DependencyObject, DependencyPropertyChangedEventArgs)
        if (firstParamType.Contains("DependencyObject") &&
            secondParamType.Contains("DependencyPropertyChangedEventArgs"))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.WA006_ConvertPropertyChangedCallback,
                methodDeclaration.Identifier.GetLocation(),
                methodDeclaration.Identifier.Text);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
