using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Helpers;

public static class RoutedEventSyntaxHelper
{
    public static ExpressionSyntax? CreateClassHandlerDelegate(ExpressionSyntax handlerExpression)
    {
        handlerExpression = handlerExpression switch
        {
            ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Arguments.Count == 1 =>
                objectCreation.ArgumentList.Arguments[0].Expression,
            ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Arguments.Count > 1 =>
                objectCreation,
            _ => handlerExpression
        };

        if (handlerExpression is ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax)
        {
            return handlerExpression;
        }

        var senderParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender"));
        var argsParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("args"));

        InvocationExpressionSyntax? handlerInvocation = handlerExpression switch
        {
            IdentifierNameSyntax identifier => SyntaxFactory.InvocationExpression(
                identifier.WithoutTrivia(),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("sender")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("args"))
                    }))),
            MemberAccessExpressionSyntax memberAccess => SyntaxFactory.InvocationExpression(
                memberAccess.WithoutTrivia(),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("sender")),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("args"))
                    }))),
            _ => null
        };

        if (handlerInvocation == null)
            return null;

        return SyntaxFactory.ParenthesizedLambdaExpression(handlerInvocation)
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { senderParameter, argsParameter })));
    }

    public static ExpressionSyntax? NormalizeHandlerExpression(ExpressionSyntax expression)
    {
        return expression switch
        {
            ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Arguments.Count == 1 =>
                objectCreation.ArgumentList.Arguments[0].Expression.WithoutTrivia(),
            ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Arguments.Count == 0 =>
                objectCreation,
            ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax => expression,
            _ => expression.WithoutTrivia()
        };
    }
}
