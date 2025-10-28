using System;
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

    private static readonly FixStep RemoveTelemetryStep = new(
        "RemoveTelemetryInstrumentation",
        requiresSemanticModel: false,
        apply: (root, model) => TelemetryInstrumentationService.RemoveTelemetryInstrumentation(root, model));

    private static readonly FixStep RemoveCommonDependencyPropertyStep = new(
        "RemoveCommonDependencyPropertyAttribute",
        requiresSemanticModel: false,
        apply: (root, model) => CommonDependencyPropertyAttributeService.RemoveCommonDependencyPropertyAttributes(root, model));

    private static readonly FixStep ConvertDependencyPropertiesStep = new(
        "ConvertDependencyProperties",
        requiresSemanticModel: true,
        apply: (root, model) => ConvertDependencyProperties(root, model!));

    private static readonly FixStep ConvertFrameworkMetadataStep = new(
        "ConvertFrameworkPropertyMetadata",
        requiresSemanticModel: true,
        apply: (root, model) => ConvertFrameworkPropertyMetadata(root, model!));

    private static readonly FixStep ConvertPropertyChangedCallbacksStep = new(
        "ConvertPropertyChangedCallbacks",
        requiresSemanticModel: true,
        apply: (root, model) => ConvertPropertyChangedCallbacks(root, model!));

    private static readonly FixStep RemoveEffectiveValuesInitialSizeStep = new(
        "RemoveEffectiveValuesInitialSizeOverrides",
        requiresSemanticModel: false,
        apply: (root, model) => RemoveEffectiveValuesInitialSizeOverrides(root, model));

    private static readonly FixStep RemoveGetValueCastsStep = new(
        "RemoveGetValueCasts",
        requiresSemanticModel: false,
        apply: (root, model) => RemoveGetValueCasts(root));

    private static readonly FixStep UpdateBaseClassesStep = new(
        "UpdateBaseClasses",
        requiresSemanticModel: false,
        apply: (root, model) => UpdateBaseClasses(root));

    private static readonly FixStep UpdateUsingsStep = new(
        "UpdateUsings",
        requiresSemanticModel: false,
        apply: (root, model) => UpdateUsings(root));

    private static readonly FixPass[] FixPasses = new[]
    {
        new FixPass(
            "PreProcessing",
            new[] { RemoveTelemetryStep, RemoveCommonDependencyPropertyStep },
            maxIterations: 3),
        new FixPass(
            "CoreConversions",
            new[] { ConvertFrameworkMetadataStep, ConvertDependencyPropertiesStep, ConvertPropertyChangedCallbacksStep, RemoveEffectiveValuesInitialSizeStep },
            maxIterations: 3),
        new FixPass(
            "FinalPolish",
            new[] { RemoveGetValueCastsStep, UpdateBaseClassesStep, UpdateUsingsStep },
            maxIterations: 2)
    };

    private static SyntaxNode ApplyAllFixes(SyntaxNode root, SemanticModel? semanticModel)
    {
        var currentRoot = root;
        var currentSemanticModel = semanticModel;

        foreach (var pass in FixPasses)
        {
            currentRoot = RunPass(pass, currentRoot, ref currentSemanticModel);
        }

        return currentRoot;
    }

    private static SyntaxNode RunPass(FixPass pass, SyntaxNode root, ref SemanticModel? semanticModel)
    {
        var current = root;

        for (var iteration = 0; iteration < pass.MaxIterations; iteration++)
        {
            var changed = false;

            foreach (var step in pass.Steps)
            {
                if (step.RequiresSemanticModel && semanticModel == null)
                    continue;

                var next = ApplyStep(step, current, semanticModel);
                if (!ReferenceEquals(next, current))
                {
                    var previous = current;
                    current = next;
                    semanticModel = RefreshSemanticModel(semanticModel, previous, current);
                    if (semanticModel != null)
                    {
                        current = semanticModel.SyntaxTree.GetRoot();
                    }
                    changed = true;
                }
            }

            if (!changed)
                break;
        }

        return current;
    }

    private static SyntaxNode ApplyStep(FixStep step, SyntaxNode root, SemanticModel? semanticModel)
    {
        var current = root;

        for (var iteration = 0; iteration < step.MaxIterations; iteration++)
        {
            var next = step.Apply(current, step.RequiresSemanticModel ? semanticModel! : null);
            if (ReferenceEquals(next, current))
                break;

            current = next;
        }

        return current;
    }

    private static SemanticModel? RefreshSemanticModel(SemanticModel? semanticModel, SyntaxNode previousRoot, SyntaxNode updatedRoot)
    {
        if (semanticModel == null)
            return null;

        var oldTree = semanticModel.SyntaxTree;
        var currentTree = updatedRoot.SyntaxTree;
        if (oldTree == currentTree)
            return semanticModel;

        SyntaxTree newTree;
        if (oldTree is CSharpSyntaxTree && updatedRoot is CSharpSyntaxNode csharpRoot && oldTree.Options is CSharpParseOptions csharpOptions)
        {
            newTree = CSharpSyntaxTree.Create(csharpRoot, csharpOptions, oldTree.FilePath, oldTree.Encoding);
        }
        else
        {
            newTree = oldTree.WithRootAndOptions(updatedRoot, oldTree.Options);
        }

        var compilation = semanticModel.Compilation;
        var newCompilation = compilation.ReplaceSyntaxTree(oldTree, newTree);
        return newCompilation.GetSemanticModel(newTree);
    }

    private sealed class FixStep
    {
        public FixStep(
            string name,
            bool requiresSemanticModel,
            Func<SyntaxNode, SemanticModel?, SyntaxNode> apply,
            int maxIterations = 1)
        {
            Name = name;
            RequiresSemanticModel = requiresSemanticModel;
            Apply = apply;
            MaxIterations = maxIterations;
        }

        public string Name { get; }
        public bool RequiresSemanticModel { get; }
        public Func<SyntaxNode, SemanticModel?, SyntaxNode> Apply { get; }
        public int MaxIterations { get; }
    }

    private sealed class FixPass
    {
        public FixPass(
            string name,
            FixStep[] steps,
            int maxIterations = 1)
        {
            Name = name;
            Steps = steps;
            MaxIterations = maxIterations;
        }

        public string Name { get; }
        public FixStep[] Steps { get; }
        public int MaxIterations { get; }
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

            currentRoot = DependencyPropertyService.ConvertDependencyPropertyToStyledProperty(currentRoot, currentVariable, semanticModel);
        }

        return currentRoot;
    }

    private static SyntaxNode ConvertFrameworkPropertyMetadata(SyntaxNode root, SemanticModel semanticModel)
    {
        var variables = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Where(variable => ShouldConvertFrameworkMetadata(variable, semanticModel))
            .ToList();

        if (variables.Count == 0)
            return root;

        var currentRoot = root.TrackNodes(variables);

        foreach (var variable in variables)
        {
            var currentVariable = currentRoot.GetCurrentNode(variable);
            if (currentVariable == null)
                continue;

            currentRoot = DependencyPropertyService.ConvertDependencyPropertyToStyledProperty(currentRoot, currentVariable, semanticModel);
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

    private static bool ShouldConvertFrameworkMetadata(VariableDeclaratorSyntax variable, SemanticModel semanticModel)
    {
        if (!ShouldConvertDependencyProperty(variable, semanticModel))
            return false;

        if (variable.Initializer?.Value is not InvocationExpressionSyntax invocation ||
            invocation.ArgumentList.Arguments.Count < 4)
        {
            return false;
        }

        var metadataExpression = invocation.ArgumentList.Arguments[3].Expression;
        var typeInfo = semanticModel.GetTypeInfo(metadataExpression);
        return typeInfo.Type?.Name == "FrameworkPropertyMetadata" &&
               typeInfo.Type.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }

    private static SyntaxNode RemoveEffectiveValuesInitialSizeOverrides(SyntaxNode root, SemanticModel? semanticModel)
    {
        var properties = root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .Where(static property =>
                property.Identifier.Text == "EffectiveValuesInitialSize" &&
                property.Modifiers.Any(SyntaxKind.OverrideKeyword))
            .ToList();

        if (properties.Count == 0)
            return root;

        var candidates = new List<PropertyDeclarationSyntax>();

        foreach (var property in properties)
        {
            if (semanticModel != null && semanticModel.SyntaxTree == property.SyntaxTree)
            {
                var symbol = semanticModel.GetDeclaredSymbol(property);
                if (symbol?.IsOverride != true)
                    continue;
            }

            candidates.Add(property);
        }

        if (candidates.Count == 0)
            return root;

        return root.RemoveNodes(candidates, SyntaxRemoveOptions.KeepNoTrivia) ?? root;
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
