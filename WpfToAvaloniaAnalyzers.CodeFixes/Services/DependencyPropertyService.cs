using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class DependencyPropertyService
{
    public static SyntaxNode ConvertDependencyPropertyToStyledProperty(
        SyntaxNode root,
        VariableDeclaratorSyntax fieldVariable)
    {
        var fieldDeclaration = fieldVariable.Parent?.Parent as FieldDeclarationSyntax;
        if (fieldDeclaration == null)
            return root;

        // Extract information from the DependencyProperty.Register call
        if (fieldVariable.Initializer?.Value is not InvocationExpressionSyntax invocation)
            return root;

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 3)
            return root;

        // Get property name, property type, and owner type
        var propertyNameArg = arguments[0].Expression;
        var propertyTypeArg = arguments[1].Expression;
        var ownerTypeArg = arguments[2].Expression;

        // Extract the type from typeof() expressions
        TypeSyntax? propertyType = null;
        TypeSyntax? ownerType = null;

        if (propertyTypeArg is TypeOfExpressionSyntax propertyTypeofExpr)
        {
            propertyType = propertyTypeofExpr.Type;
        }

        if (ownerTypeArg is TypeOfExpressionSyntax ownerTypeofExpr)
        {
            ownerType = ownerTypeofExpr.Type;
        }

        // Extract default value and property changed callback from PropertyMetadata if present (4th argument)
        ExpressionSyntax? defaultValue = null;
        ExpressionSyntax? propertyChangedCallback = null;

        if (arguments.Count >= 4)
        {
            var metadataArg = arguments[3].Expression;
            // Check if it's new PropertyMetadata(defaultValue) or new PropertyMetadata(defaultValue, callback)
            if (metadataArg is ObjectCreationExpressionSyntax objectCreation &&
                objectCreation.ArgumentList?.Arguments.Count > 0)
            {
                defaultValue = objectCreation.ArgumentList.Arguments[0].Expression;

                // Check for property changed callback (2nd argument in PropertyMetadata)
                if (objectCreation.ArgumentList.Arguments.Count >= 2)
                {
                    propertyChangedCallback = objectCreation.ArgumentList.Arguments[1].Expression;
                }
            }
        }

        // Create new Avalonia StyledProperty<T> field declaration
        TypeSyntax newType;
        if (propertyType != null)
        {
            // Create generic type: StyledProperty<T>
            newType = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("StyledProperty"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(propertyType)))
                .WithTrailingTrivia(SyntaxFactory.Space);
        }
        else
        {
            // Fallback to non-generic if we can't extract the type
            newType = SyntaxFactory.ParseTypeName("StyledProperty")
                .WithTrailingTrivia(SyntaxFactory.Space);
        }

        // Build the AvaloniaProperty.Register<TOwner, TValue>(name, defaultValue) call
        InvocationExpressionSyntax registerCall;

        if (propertyType != null && ownerType != null)
        {
            // Create generic method call: AvaloniaProperty.Register<TOwner, TValue>
            var genericRegister = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Register"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(new[] { ownerType, propertyType })));

            var registerArgs = new List<ArgumentSyntax> { SyntaxFactory.Argument(propertyNameArg) };

            // Add default value if available, otherwise use default(T)
            if (defaultValue != null)
            {
                registerArgs.Add(SyntaxFactory.Argument(defaultValue));
            }
            else
            {
                // Use default(TValue) as the default value
                var defaultExpr = SyntaxFactory.DefaultExpression(propertyType);
                registerArgs.Add(SyntaxFactory.Argument(defaultExpr));
            }

            // Add notify parameter if there's a property changed callback
            // In Avalonia: AvaloniaProperty.Register<TOwner, TValue>(name, defaultValue, notify: OnPropertyChanged)
            if (propertyChangedCallback != null)
            {
                registerArgs.Add(SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("notify"),
                    SyntaxFactory.Token(SyntaxKind.None),
                    ConvertWpfCallbackToAvaloniaNotify(propertyChangedCallback)));
            }

            registerCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("AvaloniaProperty"),
                    genericRegister))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(registerArgs)));
        }
        else
        {
            // Fallback to non-generic if we can't extract types
            registerCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("AvaloniaProperty"),
                    SyntaxFactory.IdentifierName("Register")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(new[]
                        {
                            SyntaxFactory.Argument(propertyNameArg)
                        })));
        }

        var newVariable = fieldVariable
            .WithInitializer(
                SyntaxFactory.EqualsValueClause(registerCall));

        var newVariableDeclaration = fieldDeclaration.Declaration
            .WithType(newType)
            .WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable));

        var newFieldDeclaration = fieldDeclaration
            .WithDeclaration(newVariableDeclaration);

        // Replace the field declaration
        return root.ReplaceNode(fieldDeclaration, newFieldDeclaration);
    }

    private static ExpressionSyntax ConvertWpfCallbackToAvaloniaNotify(ExpressionSyntax wpfCallback)
    {
        // WPF signature: static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        // Avalonia signature: static void OnPropertyChanged(AvaloniaObject sender, bool before)
        // For now, we'll create a lambda that adapts the call:
        // notify: (sender, before) => { if (!before) OnPropertyChanged(sender, default); }

        if (wpfCallback is IdentifierNameSyntax callbackName)
        {
            // Create: (sender, before) => { if (!before) CallbackName(sender, default); }
            var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender")),
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier("before"))
                    })),
                SyntaxFactory.Block(
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.PrefixUnaryExpression(
                            SyntaxKind.LogicalNotExpression,
                            SyntaxFactory.IdentifierName("before")),
                        SyntaxFactory.ExpressionStatement(
                            SyntaxFactory.InvocationExpression(
                                callbackName,
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList(new[]
                                    {
                                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName("sender")),
                                        SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(
                                            SyntaxFactory.IdentifierName("DependencyPropertyChangedEventArgs")))
                                    })))))));

            return lambda;
        }

        // For lambda callbacks or other complex cases, just return the original for now
        return wpfCallback;
    }
}
