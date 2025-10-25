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

        // Try to infer the property type from usage in the method body
        var propertyType = InferPropertyTypeFromMethodBody(methodDeclaration, secondParam);

        // Create new parameters with Avalonia types
        // Preserve the original type's trivia
        var newFirstParamType = SyntaxFactory.IdentifierName("AvaloniaObject")
            .WithLeadingTrivia(firstParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(firstParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var newFirstParam = firstParam.WithType(newFirstParamType);

        // Create AvaloniaPropertyChangedEventArgs<T>
        var newSecondParamType = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("AvaloniaPropertyChangedEventArgs"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(propertyType)))
            .WithLeadingTrivia(secondParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(secondParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var newSecondParam = secondParam.WithType(newSecondParamType);

        // Create new parameter list preserving trivia from the original
        var newParameterList = methodDeclaration.ParameterList
            .WithParameters(SyntaxFactory.SeparatedList(new[] { newFirstParam, newSecondParam }));

        var newMethodDeclaration = methodDeclaration.WithParameterList(newParameterList);

        return root.ReplaceNode(methodDeclaration, newMethodDeclaration);
    }

    private static TypeSyntax InferPropertyTypeFromMethodBody(
        MethodDeclarationSyntax methodDeclaration,
        ParameterSyntax secondParam)
    {
        // Default to object if we can't infer
        TypeSyntax propertyType = SyntaxFactory.IdentifierName("object");

        // Look for patterns like: (int)e.NewValue, (string)e.OldValue
        if (methodDeclaration.Body != null)
        {
            var casts = methodDeclaration.Body.DescendantNodes()
                .OfType<CastExpressionSyntax>()
                .Where(c => c.Expression is MemberAccessExpressionSyntax mae &&
                           mae.Expression is IdentifierNameSyntax id &&
                           id.Identifier.Text == secondParam.Identifier.Text &&
                           (mae.Name.Identifier.Text == "NewValue" || mae.Name.Identifier.Text == "OldValue"))
                .FirstOrDefault();

            if (casts != null)
            {
                propertyType = casts.Type;
            }
        }

        return propertyType;
    }
}
