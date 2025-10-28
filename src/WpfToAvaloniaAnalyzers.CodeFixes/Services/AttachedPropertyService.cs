using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

internal static class AttachedPropertyService
{
    public static TypeSyntax DetermineTargetType(
        ClassDeclarationSyntax containingClass,
        string propertyBaseName,
        SemanticModel? semanticModel) => SyntaxFactory.IdentifierName("AvaloniaObject");

    public static SyntaxNode UpdateAccessors(
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string propertyFieldName,
        string propertyBaseName,
        TypeSyntax targetType,
        TypeSyntax valueType)
    {
        var updatedRoot = root;

        var getMethod = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, "Get" + propertyBaseName, StringComparison.Ordinal) &&
                                 m.ParameterList.Parameters.Count == 1);

        var setMethod = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, "Set" + propertyBaseName, StringComparison.Ordinal) &&
                                 m.ParameterList.Parameters.Count == 2);

        if (getMethod != null)
        {
            var newGetMethod = UpdateGetMethod(getMethod, propertyFieldName, targetType, valueType);
            updatedRoot = updatedRoot.ReplaceNode(getMethod, newGetMethod);

            classDeclaration = updatedRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(cd => cd.Identifier.Text == classDeclaration.Identifier.Text);
        }

        if (setMethod != null)
        {
            var newSetMethod = UpdateSetMethod(setMethod, propertyFieldName, targetType);
            updatedRoot = updatedRoot.ReplaceNode(setMethod, newSetMethod);

            classDeclaration = updatedRoot.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(cd => cd.Identifier.Text == classDeclaration.Identifier.Text);
        }

        classDeclaration = updatedRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == classDeclaration.Identifier.Text);

        var refreshedSetMethod = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, "Set" + propertyBaseName, StringComparison.Ordinal) &&
                                 m.ParameterList.Parameters.Count >= 1);

        if (refreshedSetMethod != null)
        {
            var firstParameter = refreshedSetMethod.ParameterList.Parameters.First();
            if (firstParameter.Type is IdentifierNameSyntax id && id.Identifier.Text == "UIElement")
            {
                var replacementType = SyntaxFactory.IdentifierName("AvaloniaObject")
                    .WithLeadingTrivia(firstParameter.Type!.GetLeadingTrivia())
                    .WithTrailingTrivia(firstParameter.Type.GetTrailingTrivia());

                var replacement = firstParameter.WithType(replacementType);

                var parameterList = refreshedSetMethod.ParameterList.ReplaceNode(firstParameter, replacement);
                var newMethod = refreshedSetMethod.WithParameterList(parameterList);
                updatedRoot = updatedRoot.ReplaceNode(refreshedSetMethod, newMethod);
            }
        }

        classDeclaration = updatedRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First(cd => cd.Identifier.Text == classDeclaration.Identifier.Text);

        var callbackMethods = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(m => m.ParameterList.Parameters.Count == 2)
            .Where(m => IsWpfCallbackSignature(m))
            .ToList();

        foreach (var callback in callbackMethods)
        {
            var firstParam = callback.ParameterList.Parameters[0];
            var secondParam = callback.ParameterList.Parameters[1];

            var newFirstType = SyntaxFactory.IdentifierName("AvaloniaObject")
                .WithLeadingTrivia(firstParam.Type!.GetLeadingTrivia())
                .WithTrailingTrivia(firstParam.Type.GetTrailingTrivia());

            var newSecondType = SyntaxFactory.IdentifierName("AvaloniaPropertyChangedEventArgs")
                .WithLeadingTrivia(secondParam.Type!.GetLeadingTrivia())
                .WithTrailingTrivia(secondParam.Type.GetTrailingTrivia());

            var newFirstParam = firstParam.WithType(newFirstType);
            var newSecondParam = secondParam.WithType(newSecondType);

            var newParameterList = callback.ParameterList.WithParameters(
                SyntaxFactory.SeparatedList(new[] { newFirstParam, newSecondParam }));

            var newCallback = callback.WithParameterList(newParameterList);
            updatedRoot = updatedRoot.ReplaceNode(callback, newCallback);
        }

        return updatedRoot;
    }

    private static bool IsWpfCallbackSignature(MethodDeclarationSyntax method)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count != 2)
            return false;

        var firstParam = parameters[0].Type?.ToString();
        var secondParam = parameters[1].Type?.ToString();

        if (firstParam == null || secondParam == null)
            return false;

        return firstParam.IndexOf("DependencyObject", StringComparison.Ordinal) >= 0 &&
               secondParam.IndexOf("DependencyPropertyChangedEventArgs", StringComparison.Ordinal) >= 0;
    }

    private static MethodDeclarationSyntax UpdateGetMethod(
        MethodDeclarationSyntax method,
        string propertyFieldName,
        TypeSyntax targetType,
        TypeSyntax valueType)
    {
        var cleanedMethod = RemoveAttachedPropertyAttributes(method);

        cleanedMethod = EnsureBlockBody(cleanedMethod);

        var parameter = cleanedMethod.ParameterList.Parameters.First();
        var targetTypeWithTrivia = parameter.Type != null
            ? targetType.WithTriviaFrom(parameter.Type!)
            : targetType;

        var newParameter = parameter.WithType(targetTypeWithTrivia);

        var newParameterList = SyntaxFactory.ParameterList(
            SyntaxFactory.SingletonSeparatedList(newParameter));

        var statements = GetPreservedStatements(cleanedMethod.Body);

        var returnStatement = SyntaxFactory.ReturnStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(newParameter.Identifier.Text),
                    SyntaxFactory.IdentifierName("GetValue")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(propertyFieldName))))));

        statements.Add(returnStatement);

        cleanedMethod = cleanedMethod
            .WithParameterList(newParameterList)
            .WithBody(SyntaxFactory.Block(statements));

        if (!cleanedMethod.ReturnType.IsEquivalentTo(valueType, topLevel: false))
        {
            cleanedMethod = cleanedMethod.WithReturnType(valueType.WithTriviaFrom(cleanedMethod.ReturnType));
        }

        return cleanedMethod;
    }

    private static MethodDeclarationSyntax UpdateSetMethod(
        MethodDeclarationSyntax method,
        string propertyFieldName,
        TypeSyntax targetType)
    {
        var cleanedMethod = RemoveAttachedPropertyAttributes(method);
        cleanedMethod = EnsureBlockBody(cleanedMethod);

        var parameters = cleanedMethod.ParameterList.Parameters;
        if (parameters.Count < 2)
            return cleanedMethod;

        var targetParameter = parameters[0];
        var valueParameter = parameters[1];

        var targetTypeWithTrivia = targetParameter.Type != null
            ? targetType.WithTriviaFrom(targetParameter.Type!)
            : targetType;

        var newTargetParameter = targetParameter.WithType(targetTypeWithTrivia);

        var updatedParameters = SyntaxFactory.SeparatedList(new[]
        {
            newTargetParameter,
            valueParameter
        });

        var newParameterList = SyntaxFactory.ParameterList(updatedParameters);

        var statements = GetPreservedStatements(cleanedMethod.Body);

        var setStatement = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(newTargetParameter.Identifier.Text),
                    SyntaxFactory.IdentifierName("SetValue")))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(propertyFieldName)),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(valueParameter.Identifier.Text))
                    }))));

        statements.Add(setStatement);

        cleanedMethod = cleanedMethod
            .WithParameterList(newParameterList)
            .WithBody(SyntaxFactory.Block(statements));

        return cleanedMethod;
    }

    private static MethodDeclarationSyntax EnsureBlockBody(MethodDeclarationSyntax method)
    {
        if (method.Body != null)
            return method.WithExpressionBody(null).WithSemicolonToken(default);

        if (method.ExpressionBody != null)
        {
            var expressionStatement = SyntaxFactory.ExpressionStatement(method.ExpressionBody.Expression);
            return method.WithExpressionBody(null)
                .WithSemicolonToken(default)
                .WithBody(SyntaxFactory.Block(expressionStatement));
        }

        return method.WithBody(SyntaxFactory.Block());
    }

    private static List<StatementSyntax> GetPreservedStatements(BlockSyntax? body)
    {
        var statements = new List<StatementSyntax>();

        if (body == null)
            return statements;

        foreach (var statement in body.Statements)
        {
            if (IsThrowIfNullStatement(statement))
            {
                statements.Add(statement);
            }
        }

        return statements;
    }

    private static bool IsThrowIfNullStatement(StatementSyntax statement)
    {
        if (statement is ExpressionStatementSyntax exprStmt &&
            exprStmt.Expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax identifier &&
            identifier.Identifier.Text == "ArgumentNullException" &&
            memberAccess.Name.Identifier.Text == "ThrowIfNull")
        {
            return true;
        }

        return false;
    }

    private static MethodDeclarationSyntax RemoveAttachedPropertyAttributes(MethodDeclarationSyntax method)
    {
        if (method.AttributeLists.Count == 0)
            return method;

        var newAttributeLists = new List<AttributeListSyntax>();
        foreach (var attrList in method.AttributeLists)
        {
            var remaining = attrList.Attributes
                .Where(attr => !IsAttachedPropertyBrowsableAttribute(attr))
                .ToList();

            if (remaining.Count == 0)
                continue;

            newAttributeLists.Add(attrList.WithAttributes(SyntaxFactory.SeparatedList(remaining)));
        }

        return method.WithAttributeLists(SyntaxFactory.List(newAttributeLists));
    }

    private static bool IsAttachedPropertyBrowsableAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name.IndexOf("AttachedPropertyBrowsable", StringComparison.Ordinal) >= 0;
    }
}
