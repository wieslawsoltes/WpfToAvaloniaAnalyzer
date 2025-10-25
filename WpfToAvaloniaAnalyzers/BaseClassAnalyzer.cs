using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BaseClassAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA003_ConvertWpfBaseClass);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (classDeclaration.BaseList == null)
            return;

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            // Get semantic info to check if it's WPF Control
            var typeInfo = context.SemanticModel.GetTypeInfo(baseType.Type, context.CancellationToken);
            if (typeInfo.Type != null)
            {
                var typeNamespace = typeInfo.Type.ContainingNamespace?.ToDisplayString();
                var typeName = typeInfo.Type.Name;

                // Check if it's System.Windows.Controls.Control
                if (typeName == "Control" && typeNamespace == "System.Windows.Controls")
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.WA003_ConvertWpfBaseClass,
                        baseType.Type.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
