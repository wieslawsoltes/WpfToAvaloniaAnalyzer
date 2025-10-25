using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class PropertyChangedCallbackService
{
    public static SyntaxNode ConvertCallbackSignatureToAvalonia(
        SyntaxNode root,
        MethodDeclarationSyntax methodDeclaration,
        SemanticModel? semanticModel = null)
    {
        var parameters = methodDeclaration.ParameterList.Parameters;
        if (parameters.Count != 2)
            return root;

        var firstParam = parameters[0];
        var secondParam = parameters[1];

        // Create new parameters with Avalonia types
        // Preserve the original type's trivia
        var newFirstParamType = SyntaxFactory.IdentifierName("AvaloniaObject")
            .WithLeadingTrivia(firstParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(firstParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var newFirstParam = firstParam.WithType(newFirstParamType);

        var newSecondParamType = SyntaxFactory.IdentifierName("AvaloniaPropertyChangedEventArgs")
            .WithLeadingTrivia(secondParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(secondParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var newSecondParam = secondParam.WithType(newSecondParamType);

        // Create new parameter list preserving trivia from the original
        var newParameterList = methodDeclaration.ParameterList
            .WithParameters(SyntaxFactory.SeparatedList(new[] { newFirstParam, newSecondParam }));

        var newMethodDeclaration = methodDeclaration.WithParameterList(newParameterList);

        var updatedRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

        if (updatedRoot is CompilationUnitSyntax compilationUnit)
        {
            var usings = compilationUnit.Usings.ToList();
            var hasAvaloniaUsing = usings.Any(u => u.Name?.ToString() == "Avalonia");

            if (!hasAvaloniaUsing)
            {
                usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Avalonia"))
                    .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(SyntaxFactory.LineFeed)));
                compilationUnit = compilationUnit.WithUsings(SyntaxFactory.List(usings));
            }

            updatedRoot = compilationUnit;
        }

        return updatedRoot;
    }
}
