using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WpfToAvaloniaAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WpfToAvaloniaFileAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.WA007_ApplyAllAnalyzers);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterSyntaxTreeAction(treeContext =>
            {
                AnalyzeSyntaxTree(treeContext, compilationContext);
            });
        });
    }

    private static void AnalyzeSyntaxTree(
        SyntaxTreeAnalysisContext context,
        CompilationStartAnalysisContext compilationContext)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var semanticModel = compilationContext.Compilation.GetSemanticModel(context.Tree);
        var location = FindFirstIssue(root, semanticModel, context.CancellationToken);

        if (location != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.WA007_ApplyAllAnalyzers, location));
        }
    }

    private static Location? FindFirstIssue(SyntaxNode root, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            var name = usingDirective.Name?.ToString();
            if (name == "System.Windows" || name == "System.Windows.Controls")
            {
                return usingDirective.GetLocation();
            }
        }

        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var location = GetDependencyPropertyIssueLocation(field, semanticModel, cancellationToken);
            if (location != null)
            {
                return location;
            }

            var attachedLocation = GetAttachedPropertyIssueLocation(field, semanticModel, cancellationToken);
            if (attachedLocation != null)
            {
                return attachedLocation;
            }
        }

        foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var location = GetWpfBaseClassLocation(classDeclaration, semanticModel, cancellationToken);
            if (location != null)
            {
                return location;
            }
        }

        foreach (var castExpression in root.DescendantNodes().OfType<CastExpressionSyntax>())
        {
            if (IsGetValueCast(castExpression, semanticModel, cancellationToken))
            {
                return castExpression.GetLocation();
            }
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (HasWpfPropertyChangedSignature(method, semanticModel, cancellationToken))
            {
                return method.Identifier.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetDependencyPropertyIssueLocation(
        FieldDeclarationSyntax fieldDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return null;
        }

        var variableDeclaration = fieldDeclaration.Declaration;
        var typeInfo = semanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken);
        if (typeInfo.Type == null ||
            typeInfo.Type.Name != "DependencyProperty" ||
            typeInfo.Type.ContainingNamespace?.ToDisplayString() != "System.Windows")
        {
            return null;
        }

        foreach (var variable in variableDeclaration.Variables)
        {
            if (!variable.Identifier.Text.EndsWith("Property"))
                continue;

            if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation)
                continue;

            var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.Name == "Register" &&
                methodSymbol.ContainingType?.Name == "DependencyProperty" &&
                methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows")
            {
                return variable.Identifier.GetLocation();
            }
        }

        return null;
    }

    private static Location? GetWpfBaseClassLocation(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (classDeclaration.BaseList == null)
            return null;

        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var typeInfo = semanticModel.GetTypeInfo(baseType.Type, cancellationToken);
            if (typeInfo.Type != null &&
                typeInfo.Type.Name == "Control" &&
                typeInfo.Type.ContainingNamespace?.ToDisplayString() == "System.Windows.Controls")
            {
                return baseType.Type.GetLocation();
            }
        }

        return null;
    }

    private static bool IsGetValueCast(
        CastExpressionSyntax castExpression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (castExpression.Expression is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != "GetValue")
        {
            return false;
        }

        var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
        return symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.Name == "GetValue";
    }

    private static bool HasWpfPropertyChangedSignature(
        MethodDeclarationSyntax methodDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (methodDeclaration.ParameterList.Parameters.Count != 2)
            return false;

        var firstParam = methodDeclaration.ParameterList.Parameters[0];
        var secondParam = methodDeclaration.ParameterList.Parameters[1];

        var firstParamSymbol = semanticModel.GetDeclaredSymbol(firstParam, cancellationToken);
        var secondParamSymbol = semanticModel.GetDeclaredSymbol(secondParam, cancellationToken);

        if (firstParamSymbol?.Type == null || secondParamSymbol?.Type == null)
            return false;

        var firstParamType = firstParamSymbol.Type.ToDisplayString();
        var secondParamType = secondParamSymbol.Type.ToDisplayString();

        return firstParamType.Contains("DependencyObject") &&
               secondParamType.Contains("DependencyPropertyChangedEventArgs");
    }

    private static Location? GetAttachedPropertyIssueLocation(
        FieldDeclarationSyntax fieldDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return null;
        }

        if (fieldDeclaration.Declaration.Type is not IdentifierNameSyntax identifier ||
            identifier.Identifier.Text != "DependencyProperty")
        {
            return null;
        }

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation)
                continue;

            var symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
                continue;

            if (methodSymbol.Name == "RegisterAttached" &&
                methodSymbol.ContainingType?.Name == "DependencyProperty" &&
                methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows")
            {
                return variable.Identifier.GetLocation();
            }
        }

        return null;
    }
}
