using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class WpfToAvaloniaBatchService
{
    public static async Task<Document> ApplyAllFixesAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var updatedRoot = ApplyAllFixes(root, semanticModel);

        return updatedRoot == root
            ? document
            : document.WithSyntaxRoot(updatedRoot);
    }

    private static SyntaxNode ApplyAllFixes(SyntaxNode root, SemanticModel? semanticModel)
    {
        var currentRoot = root;

        if (semanticModel != null)
        {
            currentRoot = ConvertDependencyProperties(currentRoot, semanticModel);
            currentRoot = ConvertPropertyChangedCallbacks(currentRoot, semanticModel);
        }

        currentRoot = RemoveGetValueCasts(currentRoot);
        currentRoot = UpdateBaseClasses(currentRoot);
        currentRoot = TelemetryInstrumentationService.RemoveTelemetryInstrumentation(currentRoot, semanticModel);
        currentRoot = UpdateUsings(currentRoot);

        return currentRoot;
    }

    private static SyntaxNode ConvertDependencyProperties(SyntaxNode root, SemanticModel semanticModel)
    {
        var variables = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Where(variable => ShouldConvertDependencyProperty(variable, semanticModel))
            .ToList();

        if (variables.Count == 0)
            return root;

        var currentRoot = root.TrackNodes(variables);

        foreach (var variable in variables)
        {
            var currentVariable = currentRoot.GetCurrentNode(variable);
            if (currentVariable == null)
                continue;

            currentRoot = DependencyPropertyService.ConvertDependencyPropertyToStyledProperty(currentRoot, currentVariable);
        }

        return currentRoot;
    }

    private static SyntaxNode ConvertPropertyChangedCallbacks(SyntaxNode root, SemanticModel semanticModel)
    {
        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(ShouldConvertPropertyChangedCallback)
            .ToList();

        if (methods.Count == 0)
            return root;

        var currentRoot = root.TrackNodes(methods);

        foreach (var method in methods)
        {
            var currentMethod = currentRoot.GetCurrentNode(method);
            if (currentMethod == null)
                continue;

            currentRoot = PropertyChangedCallbackService.ConvertCallbackSignatureToAvalonia(
                currentRoot,
                currentMethod,
                semanticModel: null);
        }

        return currentRoot;
    }

    private static SyntaxNode RemoveGetValueCasts(SyntaxNode root)
    {
        return PropertyAccessorService.HasCastsOnGetValue(root)
            ? PropertyAccessorService.RemoveCastsFromGetValue(root)
            : root;
    }

    private static SyntaxNode UpdateBaseClasses(SyntaxNode root)
    {
        var classDeclarations = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(BaseClassService.HasWpfBaseClass)
            .ToList();

        if (classDeclarations.Count == 0)
            return root;

        var currentRoot = root.TrackNodes(classDeclarations);

        foreach (var classDeclaration in classDeclarations)
        {
            var currentClass = currentRoot.GetCurrentNode(classDeclaration);
            if (currentClass == null)
                continue;

            currentRoot = BaseClassService.UpdateWpfBaseClassToAvalonia(currentRoot, currentClass);
        }

        return currentRoot;
    }

    private static SyntaxNode UpdateUsings(SyntaxNode root)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return root;

        var updated = compilationUnit;

        if (UsingDirectivesService.HasWpfUsings(compilationUnit) && !UsesWpfTypes(root))
        {
            updated = UsingDirectivesService.RemoveWpfUsings(updated);
        }

        updated = UsingDirectivesService.AddAvaloniaUsings(updated);

        return updated;
    }

    private static bool ShouldConvertDependencyProperty(VariableDeclaratorSyntax variable, SemanticModel semanticModel)
    {
        if (variable.Parent is not VariableDeclarationSyntax variableDeclaration ||
            variableDeclaration.Parent is not FieldDeclarationSyntax fieldDeclaration)
        {
            return false;
        }

        if (!fieldDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword) ||
            !fieldDeclaration.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return false;
        }

        var typeInfo = semanticModel.GetTypeInfo(variableDeclaration.Type);
        if (typeInfo.Type == null)
            return false;

        if (typeInfo.Type.Name != "DependencyProperty" ||
            typeInfo.Type.ContainingNamespace?.ToDisplayString() != "System.Windows")
        {
            return false;
        }

        if (!variable.Identifier.Text.EndsWith("Property"))
            return false;

        if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation)
            return false;

        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return false;

        return methodSymbol.Name == "Register" &&
               methodSymbol.ContainingType?.Name == "DependencyProperty" &&
               methodSymbol.ContainingType?.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }

    private static bool UsesWpfTypes(SyntaxNode root)
    {
        var wpfIdentifiers = new HashSet<string>
        {
            "DependencyProperty",
            "DependencyObject",
            "DependencyPropertyChangedEventArgs",
            "PropertyMetadata"
        };

        return root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => wpfIdentifiers.Contains(identifier.Identifier.Text));
    }

    private static bool ShouldConvertPropertyChangedCallback(MethodDeclarationSyntax method)
    {
        if (method.ParameterList.Parameters.Count != 2)
            return false;

        var firstParam = method.ParameterList.Parameters[0];
        var secondParam = method.ParameterList.Parameters[1];

        if (firstParam.Type == null || secondParam.Type == null)
            return false;

        var firstParamType = firstParam.Type.ToString();
        var secondParamType = secondParam.Type.ToString();

        return firstParamType.Contains("DependencyObject") &&
               secondParamType.Contains("DependencyPropertyChangedEventArgs");
    }
}
