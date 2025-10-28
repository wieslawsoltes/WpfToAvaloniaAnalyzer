using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

internal static class TelemetryInstrumentationService
{
    private static readonly ImmutableHashSet<string> TelemetryNamespaces =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "MS.Internal.PresentationFramework",
            "MS.Internal.Telemetry.PresentationFramework");

    internal static bool IsTelemetryUsing(UsingDirectiveSyntax usingDirective) =>
        usingDirective?.Name != null &&
        TelemetryNamespaces.Contains(usingDirective.Name.ToString());

    internal static bool IsTelemetryInvocation(InvocationExpressionSyntax invocation, SemanticModel? semanticModel)
    {
        if (semanticModel != null && semanticModel.SyntaxTree == invocation.SyntaxTree)
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol != null)
            {
                return string.Equals(symbol.Name, "AddControl", StringComparison.Ordinal) &&
                       string.Equals(symbol.ContainingType?.Name, "ControlsTraceLogger", StringComparison.Ordinal) &&
                       string.Equals(
                           symbol.ContainingNamespace?.ToDisplayString(),
                           "MS.Internal.Telemetry.PresentationFramework",
                           StringComparison.Ordinal);
            }
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (!string.Equals(memberAccess.Name.Identifier.Text, "AddControl", StringComparison.Ordinal))
            return false;

        return memberAccess.Expression switch
        {
            IdentifierNameSyntax identifier => string.Equals(identifier.Identifier.Text, "ControlsTraceLogger", StringComparison.Ordinal),
            MemberAccessExpressionSyntax nested => string.Equals(nested.Name.Identifier.Text, "ControlsTraceLogger", StringComparison.Ordinal) ||
                                                   nested.ToString().EndsWith(".ControlsTraceLogger", StringComparison.Ordinal),
            _ => false
        };
    }

    internal static SyntaxNode DropUnusedTelemetryUsings(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return root;

        var telemetryUsings = compilationUnit.Usings
            .Where(IsTelemetryUsing)
            .ToList();

        if (telemetryUsings.Count == 0)
            return root;

        return compilationUnit.RemoveNodes(telemetryUsings, SyntaxRemoveOptions.KeepNoTrivia)!;
    }

    public static SyntaxNode RemoveTelemetryInstrumentation(SyntaxNode root, SemanticModel? semanticModel)
    {
        var currentRoot = root;

        var telemetryStatements = currentRoot.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .Where(statement => statement.Expression is InvocationExpressionSyntax invocation &&
                                IsTelemetryInvocation(invocation, semanticModel))
            .ToList();

        if (telemetryStatements.Count > 0)
        {
            currentRoot = currentRoot.RemoveNodes(telemetryStatements, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        var emptyStaticConstructors = currentRoot.DescendantNodes()
            .OfType<ConstructorDeclarationSyntax>()
            .Where(constructor =>
                constructor.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                constructor.ExpressionBody == null &&
                constructor.Initializer == null &&
                (constructor.Body == null || !constructor.Body.Statements.Any()))
            .ToList();

        if (emptyStaticConstructors.Count > 0)
        {
            currentRoot = currentRoot.RemoveNodes(emptyStaticConstructors, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        var telemetryUsings = currentRoot.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(IsTelemetryUsing)
            .ToList();

        if (telemetryUsings.Count > 0)
        {
            currentRoot = currentRoot.RemoveNodes(telemetryUsings, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        return currentRoot;
    }
}
