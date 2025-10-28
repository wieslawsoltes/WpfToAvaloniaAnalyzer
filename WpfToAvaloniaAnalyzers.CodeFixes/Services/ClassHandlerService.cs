using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class ClassHandlerService
{
    public static SyntaxNode AddClassHandler(
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string propertyIdentifier,
        ExpressionSyntax callbackExpression,
        TypeSyntax ownerType,
        TypeSyntax? propertyType)
    {
        var updatedRoot = root;
        var finalClass = classDeclaration;

        var callbackName = GetCallbackName(callbackExpression);
        MethodDeclarationSyntax? callbackMethod = null;

        if (!string.IsNullOrEmpty(callbackName))
        {
            callbackMethod = finalClass.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == callbackName);
        }

        if (callbackMethod == null)
        {
            callbackMethod = finalClass.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(IsWpfCallbackSignature);

            if (callbackMethod != null && string.IsNullOrEmpty(callbackName))
            {
                callbackName = callbackMethod.Identifier.Text;
            }
        }

        if (callbackMethod != null || !string.IsNullOrEmpty(callbackName))
        {
            var identifierText = !string.IsNullOrEmpty(callbackName)
                ? callbackName
                : callbackMethod?.Identifier.Text;

            if (!string.IsNullOrEmpty(identifierText))
            {
                callbackExpression = SyntaxFactory.IdentifierName(identifierText!);
            }
        }

        var handlerStatement = CreateHandlerStatement(propertyIdentifier, callbackExpression, ownerType, propertyType);

        var staticCtor = finalClass.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(ctor => ctor.Modifiers.Any(SyntaxKind.StaticKeyword) && ctor.ParameterList.Parameters.Count == 0);

        ConstructorDeclarationSyntax newStaticCtor;

        if (staticCtor != null)
        {
            var body = staticCtor.Body ??
                (staticCtor.ExpressionBody != null
                    ? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(staticCtor.ExpressionBody.Expression))
                    : SyntaxFactory.Block());

            if (!body.Statements.Any(stmt => stmt.IsEquivalentTo(handlerStatement)))
            {
                body = body.AddStatements(handlerStatement);
            }

            newStaticCtor = staticCtor.WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithBody(body);
        }
        else
        {
            newStaticCtor = SyntaxFactory.ConstructorDeclaration(finalClass.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(SyntaxFactory.Block(handlerStatement));
        }

        SyntaxList<MemberDeclarationSyntax> newMembers;
        if (staticCtor == null)
        {
            newMembers = finalClass.Members.Insert(0, newStaticCtor);
        }
        else
        {
            newMembers = finalClass.Members.Replace(staticCtor, newStaticCtor);
        }

        var newClassDeclaration = finalClass.WithMembers(newMembers);

        updatedRoot = updatedRoot.ReplaceNode(classDeclaration, newClassDeclaration);

        var updatedClassNode = updatedRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cd => cd.Identifier.Text == classDeclaration.Identifier.Text);

        if (updatedClassNode != null)
        {
            var residualMethod = updatedClassNode.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(IsWpfCallbackSignature);

            if (residualMethod != null)
            {
                var fixedMethod = UpdateCallbackSignature(residualMethod, ownerType, propertyType);
                updatedRoot = updatedRoot.ReplaceNode(residualMethod, fixedMethod);
            }
        }

        if (updatedRoot is CompilationUnitSyntax compilationUnit)
        {
            updatedRoot = UsingDirectivesService.AddAvaloniaUsings(compilationUnit);
        }

        return updatedRoot;
    }

    private static ExpressionStatementSyntax CreateHandlerStatement(
        string propertyIdentifier,
        ExpressionSyntax callbackExpression,
        TypeSyntax ownerType,
        TypeSyntax? propertyType)
    {
        var senderParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender"))
            .WithType(ownerType.WithoutTrivia());

        TypeSyntax argsParameterType = propertyType != null
            ? SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("AvaloniaPropertyChangedEventArgs"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(propertyType.WithoutTrivia())))
            : SyntaxFactory.IdentifierName("AvaloniaPropertyChangedEventArgs");

        var argsParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("args"))
            .WithType(argsParameterType);

        var handlerLambda = SyntaxFactory.ParenthesizedLambdaExpression()
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        senderParameter,
                        argsParameter
                    })))
            .WithBody(
                SyntaxFactory.InvocationExpression(
                    callbackExpression.WithoutTrivia(),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("sender")),
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("args"))
                        }))));

        SimpleNameSyntax handlerName;

        if (propertyType != null)
        {
            handlerName = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("AddClassHandler"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            ownerType.WithoutTrivia(),
                            propertyType.WithoutTrivia()
                        })));
        }
        else
        {
            handlerName = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("AddClassHandler"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(ownerType.WithoutTrivia())));
        }

        var addHandlerInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(propertyIdentifier),
                    SyntaxFactory.IdentifierName("Changed")),
                handlerName))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(handlerLambda))));

        return SyntaxFactory.ExpressionStatement(addHandlerInvocation);
    }

    private static MethodDeclarationSyntax UpdateCallbackSignature(
        MethodDeclarationSyntax method,
        TypeSyntax ownerType,
        TypeSyntax? propertyType)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return method;

        var firstParam = parameters[0];
        var secondParam = parameters[1];

        var newFirstParamType = ownerType.WithoutTrivia()
            .WithLeadingTrivia(firstParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(firstParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        TypeSyntax newSecondParamType;
        if (propertyType != null)
        {
            newSecondParamType = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("AvaloniaPropertyChangedEventArgs"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(propertyType.WithoutTrivia())));
        }
        else
        {
            newSecondParamType = SyntaxFactory.IdentifierName("AvaloniaPropertyChangedEventArgs");
        }

        newSecondParamType = newSecondParamType
            .WithLeadingTrivia(secondParam.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(secondParam.Type?.GetTrailingTrivia() ?? SyntaxFactory.TriviaList(SyntaxFactory.Space));

        var newFirstParam = firstParam.WithType(newFirstParamType);
        var newSecondParam = secondParam.WithType(newSecondParamType);

        var newParameterList = method.ParameterList.WithParameters(
            SyntaxFactory.SeparatedList(new[] { newFirstParam, newSecondParam }));

        return method.WithParameterList(newParameterList);
    }

    private static string? GetCallbackName(ExpressionSyntax callbackExpression)
    {
        return callbackExpression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    private static bool IsWpfCallbackSignature(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return false;

        var firstType = parameters[0].Type?.ToString();
        var secondType = parameters[1].Type?.ToString();

        if (firstType is null || secondType is null)
            return false;

        return firstType.Contains("DependencyObject") &&
               secondType.Contains("DependencyPropertyChangedEventArgs");
    }
}
