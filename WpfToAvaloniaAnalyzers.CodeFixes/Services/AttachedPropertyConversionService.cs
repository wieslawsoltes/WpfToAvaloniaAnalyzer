using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

internal static class AttachedPropertyConversionService
{
    public static SyntaxNode ConvertAttachedProperty(
        SyntaxNode root,
        VariableDeclaratorSyntax fieldVariable,
        SemanticModel? semanticModel)
    {
        if (fieldVariable.Parent?.Parent is not FieldDeclarationSyntax fieldDeclaration)
            return root;

        if (fieldVariable.Initializer?.Value is not InvocationExpressionSyntax invocation)
            return root;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name is not IdentifierNameSyntax methodIdentifier ||
            !string.Equals(methodIdentifier.Identifier.Text, "RegisterAttached", StringComparison.Ordinal))
        {
            return root;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 3)
            return root;

        var propertyNameArg = arguments[0].Expression;
        var propertyTypeArg = arguments[1].Expression;
        var ownerTypeArg = arguments[2].Expression;

        TypeSyntax valueType = ExtractType(propertyTypeArg) ?? SyntaxFactory.IdentifierName("object");
        TypeSyntax ownerType = ExtractType(ownerTypeArg) ?? GetContainingClassType(fieldDeclaration);

        ExpressionSyntax? defaultValue = null;
        ExpressionSyntax? propertyChangedCallback = null;
        ExpressionSyntax? validateCallback = null;
        bool inherits = false;

        if (arguments.Count >= 4)
        {
            var metadataArg = arguments[3].Expression;
            if (metadataArg is ObjectCreationExpressionSyntax metadataCreation &&
                metadataCreation.ArgumentList != null &&
                metadataCreation.ArgumentList.Arguments.Count > 0)
            {
                defaultValue = metadataCreation.ArgumentList.Arguments[0].Expression;

                if (metadataCreation.ArgumentList.Arguments.Count >= 2)
                {
                    propertyChangedCallback = UnwrapDelegate(metadataCreation.ArgumentList.Arguments[1].Expression);
                }

            if (metadataCreation.ArgumentList.Arguments.Count >= 3)
            {
                inherits = ContainsInheritsOption(metadataCreation.ArgumentList.Arguments[2].Expression, semanticModel);
            }

            if (metadataCreation.ArgumentList.Arguments.Count >= 4)
            {
                inherits = inherits || ContainsInheritsOption(metadataCreation.ArgumentList.Arguments[3].Expression, semanticModel);
            }
        }
        }

        if (arguments.Count >= 5)
        {
            validateCallback = UnwrapDelegate(arguments[4].Expression);
            if (validateCallback != null)
            {
                validateCallback = NormalizeValidateCallback(validateCallback);
            }
        }

        var containingClass = fieldDeclaration.Parent as ClassDeclarationSyntax;
        if (containingClass == null)
            return root;

        var basePropertyName = GetPropertyBaseName(fieldVariable.Identifier.Text);

        var targetType = AttachedPropertyService.DetermineTargetType(containingClass, basePropertyName, semanticModel);

        var registerArguments = new List<ArgumentSyntax> { SyntaxFactory.Argument(propertyNameArg) };

        registerArguments.Add(SyntaxFactory.Argument(defaultValue ?? SyntaxFactory.DefaultExpression(valueType)));

        if (inherits)
        {
            registerArguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("inherits"),
                    default,
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
        }

        if (validateCallback != null)
        {
            registerArguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("validate"),
                    default,
                    validateCallback));
        }

        var registerIdentifier = SyntaxFactory.GenericName(SyntaxFactory.Identifier("RegisterAttached"))
            .WithTypeArgumentList(
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        ownerType.WithoutTrivia(),
                        targetType.WithoutTrivia(),
                        valueType.WithoutTrivia()
                    })));

        var registerCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("AvaloniaProperty"),
                registerIdentifier))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(registerArguments)));

        var newValueType = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("AttachedProperty"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(valueType.WithoutTrivia())));

        var newVariable = fieldVariable.WithInitializer(SyntaxFactory.EqualsValueClause(registerCall));

        var newVariableDeclaration = fieldDeclaration.Declaration
            .WithType(newValueType)
            .WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable));

        var newFieldDeclaration = fieldDeclaration.WithDeclaration(newVariableDeclaration);

        var updatedRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);

        if (containingClass.Parent is ClassDeclarationSyntax parentClass)
        {
            containingClass = parentClass.Members
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cd => cd.Identifier.Text == containingClass.Identifier.Text) ?? containingClass;
        }

        if (updatedRoot is CompilationUnitSyntax compilationUnit)
        {
            var withoutWpf = UsingDirectivesService.RemoveWpfUsings(compilationUnit);
            updatedRoot = UsingDirectivesService.AddAvaloniaUsings(withoutWpf);
        }

        updatedRoot = BaseClassService.UpdateWpfBaseClassToAvalonia(updatedRoot);

        var updatedClass = updatedRoot.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cd => cd.Identifier.Text == containingClass.Identifier.Text);

        if (updatedClass != null)
        {
            updatedRoot = AttachedPropertyService.UpdateAccessors(
                updatedRoot,
                updatedClass,
                fieldVariable.Identifier.Text,
                basePropertyName,
                targetType,
                valueType);
        }

        if (propertyChangedCallback != null && updatedClass != null)
        {
            updatedRoot = ClassHandlerService.AddClassHandler(
                updatedRoot,
                updatedClass,
                fieldVariable.Identifier.Text,
                propertyChangedCallback,
                ownerType,
                valueType);
        }

        return updatedRoot;
    }

    private static string GetPropertyBaseName(string fieldName) =>
        fieldName.EndsWith("Property", StringComparison.Ordinal) && fieldName.Length > "Property".Length
            ? fieldName.Substring(0, fieldName.Length - "Property".Length)
            : fieldName;

    private static ExpressionSyntax? UnwrapDelegate(ExpressionSyntax expression)
    {
        if (expression is ObjectCreationExpressionSyntax objectCreation &&
            objectCreation.ArgumentList?.Arguments.Count == 1)
        {
            return objectCreation.ArgumentList.Arguments[0].Expression;
        }

        return expression;
    }

    private static ExpressionSyntax NormalizeValidateCallback(ExpressionSyntax expression)
    {
        return expression switch
        {
            SimpleLambdaExpressionSyntax => expression,
            ParenthesizedLambdaExpressionSyntax => expression,
            _ => SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("value")),
                SyntaxFactory.InvocationExpression(
                    expression.WithoutTrivia(),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value"))))))
                    .WithLeadingTrivia(expression.GetLeadingTrivia())
                    .WithTrailingTrivia(expression.GetTrailingTrivia())
        };
    }

    private static bool ContainsInheritsOption(ExpressionSyntax expression, SemanticModel? semanticModel)
    {
        if (semanticModel != null)
        {
            var constantValue = semanticModel.GetConstantValue(expression);
            if (constantValue.HasValue && constantValue.Value is int intValue)
            {
                // FrameworkPropertyMetadataOptions.Inherits == 0x00000002
                return (intValue & 0x2) == 0x2;
            }
        }

        if (expression is BinaryExpressionSyntax binary)
        {
            return ContainsInheritsOption(binary.Left, semanticModel) ||
                   ContainsInheritsOption(binary.Right, semanticModel);
        }

        if (expression is IdentifierNameSyntax identifier)
        {
            var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol?.ContainingType?.Name == "FrameworkPropertyMetadataOptions" &&
                symbol.Name == "Inherits")
            {
                return true;
            }
        }

        return false;
    }

    private static TypeSyntax? ExtractType(ExpressionSyntax expression)
    {
        if (expression is TypeOfExpressionSyntax typeOfExpression)
        {
            return typeOfExpression.Type;
        }

        return null;
    }

    private static TypeSyntax GetContainingClassType(FieldDeclarationSyntax fieldDeclaration)
    {
        if (fieldDeclaration.Parent is ClassDeclarationSyntax classDeclaration)
        {
            return SyntaxFactory.IdentifierName(classDeclaration.Identifier);
        }

        return SyntaxFactory.IdentifierName("object");
    }
}
