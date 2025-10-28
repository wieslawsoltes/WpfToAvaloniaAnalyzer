using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TelemetryInstrumentationAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> TelemetryNamespaces =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "MS.Internal.PresentationFramework",
            "MS.Internal.Telemetry.PresentationFramework");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        var usingDirective = (UsingDirectiveSyntax)context.Node;
        var usingName = usingDirective.Name?.ToString();

        if (string.IsNullOrEmpty(usingName))
            return;

        if (TelemetryNamespaces.Contains(usingName!))
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation,
                    usingDirective.GetLocation(),
                    usingName));
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol == null)
            return;

        if (symbol.Name != "AddControl")
            return;

        if (!string.Equals(symbol.ContainingType?.Name, "ControlsTraceLogger", StringComparison.Ordinal))
            return;

        if (!string.Equals(
                symbol.ContainingNamespace?.ToDisplayString(),
                "MS.Internal.Telemetry.PresentationFramework",
                StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.WA010_RemoveTelemetryInstrumentation,
                invocation.GetLocation(),
                "ControlsTraceLogger.AddControl"));
    }
}
