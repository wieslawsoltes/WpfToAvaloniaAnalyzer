using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class DependencyPropertyService
{
    public static SyntaxNode ConvertDependencyPropertyToStyledProperty(
        SyntaxNode root,
        VariableDeclaratorSyntax fieldVariable,
        SemanticModel? semanticModel = null)
    {
        semanticModel = semanticModel != null && semanticModel.SyntaxTree == fieldVariable.SyntaxTree
            ? semanticModel
            : null;

        if (fieldVariable.Parent?.Parent is not FieldDeclarationSyntax fieldDeclaration)
            return root;

        if (fieldVariable.Initializer?.Value is not InvocationExpressionSyntax invocation)
            return root;

        if (IsRegisterAttached(invocation, semanticModel))
        {
            return AttachedPropertyConversionService.ConvertAttachedProperty(root, fieldVariable, semanticModel);
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 3)
            return root;

        var propertyNameExpression = arguments[0].Expression;
        var propertyTypeExpression = arguments[1].Expression;
        var ownerTypeExpression = arguments[2].Expression;

        var propertyType = ExtractType(propertyTypeExpression);
        var ownerType = ExtractType(ownerTypeExpression);

        var metadataInfo = MetadataInfo.Create(arguments.Count >= 4 ? arguments[3].Expression : null, semanticModel);
        var rawValidateExpression = arguments.Count >= 5 ? arguments[4].Expression : null;
        var validateExpression = CreateValidateExpression(rawValidateExpression, semanticModel, propertyType);
        var coerceExpression = metadataInfo.CoerceValueCallback != null
            ? CreateCoerceLambda(metadataInfo.CoerceValueCallback, propertyType)
            : null;
        var validateMethodName = GetMethodName(rawValidateExpression);
        var coerceMethodName = GetMethodName(metadataInfo.CoerceValueCallback);

        var newFieldType = CreateStyledPropertyType(propertyType);
        var registerCall = CreateRegisterInvocation(
            propertyNameExpression,
            propertyType,
            ownerType,
            metadataInfo,
            validateExpression,
            coerceExpression);

        var newVariable = fieldVariable.WithInitializer(SyntaxFactory.EqualsValueClause(registerCall));
        var newVariableDeclaration = fieldDeclaration.Declaration
            .WithType(newFieldType)
            .WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable));

        var newFieldDeclaration = fieldDeclaration.WithDeclaration(newVariableDeclaration);

        var updatedRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);

        if (updatedRoot is CompilationUnitSyntax compilationUnit)
        {
            var withAvaloniaUsings = UsingDirectivesService.AddAvaloniaUsings(compilationUnit);
            if (metadataInfo.RequiresBindingModeUsing)
            {
                withAvaloniaUsings = UsingDirectivesService.AddAvaloniaDataUsing(withAvaloniaUsings);
            }

            updatedRoot = withAvaloniaUsings;
        }

        updatedRoot = BaseClassService.UpdateWpfBaseClassToAvalonia(updatedRoot);

        if (fieldDeclaration.Parent is not ClassDeclarationSyntax containingClass)
            return updatedRoot;

        var updatedClass = FindMatchingClass(updatedRoot, containingClass);
        if (updatedClass == null)
            return updatedRoot;

        var ownerTypeSyntax = ownerType ?? SyntaxFactory.IdentifierName(updatedClass.Identifier);

        if (metadataInfo.HasLayoutStatements)
        {
            var layoutStatements = CreateLayoutStatements(metadataInfo, ownerTypeSyntax, fieldVariable.Identifier.Text);
            updatedRoot = StaticConstructorService.AddStaticConstructorStatements(updatedRoot, updatedClass, layoutStatements);
            updatedClass = FindMatchingClass(updatedRoot, containingClass);
        }

        if (metadataInfo.PropertyChangedCallback != null && updatedClass != null)
        {
            updatedRoot = ClassHandlerService.AddClassHandler(
                updatedRoot,
                updatedClass,
                fieldVariable.Identifier.Text,
                metadataInfo.PropertyChangedCallback,
                ownerTypeSyntax,
                propertyType);

            if (updatedRoot is CompilationUnitSyntax postHandlerCompilationUnit)
            {
                updatedRoot = UsingDirectivesService.AddAvaloniaUsings(postHandlerCompilationUnit);
            }

            updatedRoot = BaseClassService.UpdateWpfBaseClassToAvalonia(updatedRoot);
            updatedClass = FindMatchingClass(updatedRoot, containingClass);
        }

        if (!string.IsNullOrEmpty(validateMethodName) && updatedClass != null)
        {
            updatedRoot = UpdateValidateMethodSignature(updatedRoot, updatedClass, validateMethodName!, propertyType);
            updatedClass = FindMatchingClass(updatedRoot, containingClass);
        }

        if (!string.IsNullOrEmpty(coerceMethodName) && updatedClass != null)
        {
            updatedRoot = UpdateCoerceMethodSignature(updatedRoot, updatedClass, coerceMethodName!, propertyType);
        }

        return updatedRoot;
    }

    private static bool IsRegisterAttached(InvocationExpressionSyntax invocation, SemanticModel? semanticModel)
    {
        var isRegisterAttached = false;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Name is IdentifierNameSyntax identifier)
        {
            isRegisterAttached = string.Equals(identifier.Identifier.Text, "RegisterAttached", StringComparison.Ordinal);
        }

        if (semanticModel != null && semanticModel.SyntaxTree == invocation.SyntaxTree)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                methodSymbol.Name == "RegisterAttached" &&
                methodSymbol.ContainingType?.Name == "DependencyProperty")
            {
                isRegisterAttached = true;
            }
        }

        return isRegisterAttached;
    }

    private static TypeSyntax? ExtractType(ExpressionSyntax expression) =>
        expression is TypeOfExpressionSyntax typeOfExpression ? typeOfExpression.Type : expression as TypeSyntax;

    private static TypeSyntax CreateStyledPropertyType(TypeSyntax? propertyType)
    {
        if (propertyType == null)
        {
            return SyntaxFactory.ParseTypeName("StyledProperty")
                .WithTrailingTrivia(SyntaxFactory.Space);
        }

        return SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("StyledProperty"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(propertyType.WithoutTrivia())))
            .WithTrailingTrivia(SyntaxFactory.Space);
    }

    private static InvocationExpressionSyntax CreateRegisterInvocation(
        ExpressionSyntax propertyNameExpression,
        TypeSyntax? propertyType,
        TypeSyntax? ownerType,
        MetadataInfo metadataInfo,
        ExpressionSyntax? validateExpression,
        ExpressionSyntax? coerceExpression)
    {
        SimpleNameSyntax memberName;

        if (propertyType != null && ownerType != null)
        {
            memberName = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("Register"),
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(new[] { ownerType.WithoutTrivia(), propertyType.WithoutTrivia() })));
        }
        else
        {
            memberName = SyntaxFactory.IdentifierName("Register");
        }

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("AvaloniaProperty"),
            memberName);

        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(propertyNameExpression)
        };

        var defaultValueArgument = metadataInfo.DefaultValue != null
            ? SyntaxFactory.Argument(metadataInfo.DefaultValue)
            : propertyType != null
                ? SyntaxFactory.Argument(SyntaxFactory.DefaultExpression(propertyType))
                : SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        arguments.Add(defaultValueArgument);

        if (metadataInfo.Inherits)
        {
            arguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("inherits"),
                    default,
                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
        }

        if (metadataInfo.BindsTwoWayByDefault)
        {
            var bindingModeExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("BindingMode"),
                SyntaxFactory.IdentifierName("TwoWay"));

            arguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("defaultBindingMode"),
                    default,
                    bindingModeExpression));
        }

        if (validateExpression != null)
        {
            arguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("validate"),
                    default,
                    validateExpression));
        }

        if (coerceExpression != null)
        {
            arguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("coerce"),
                    default,
                    coerceExpression));
        }

        return SyntaxFactory.InvocationExpression(
                memberAccess)
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(arguments)));
    }

    private static IEnumerable<StatementSyntax> CreateLayoutStatements(
        MetadataInfo metadataInfo,
        TypeSyntax ownerType,
        string propertyIdentifier)
    {
        var statements = new List<StatementSyntax>();

        if (metadataInfo.AffectsMeasure)
        {
            statements.Add(CreateAffectsStatement("AffectsMeasure", ownerType, propertyIdentifier));
        }

        if (metadataInfo.AffectsArrange)
        {
            statements.Add(CreateAffectsStatement("AffectsArrange", ownerType, propertyIdentifier));
        }

        if (metadataInfo.AffectsRender)
        {
            statements.Add(CreateAffectsStatement("AffectsRender", ownerType, propertyIdentifier));
        }

        return statements;
    }

    private static StatementSyntax CreateAffectsStatement(
        string methodName,
        TypeSyntax ownerType,
        string propertyIdentifier)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier(methodName))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(ownerType.WithoutTrivia()))))
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(propertyIdentifier)))));

        return SyntaxFactory.ExpressionStatement(invocation);
    }

    private static ExpressionSyntax? CreateValidateExpression(ExpressionSyntax? expression, SemanticModel? semanticModel, TypeSyntax? propertyType)
    {
        if (expression == null)
            return null;

        if (semanticModel != null && semanticModel.SyntaxTree != expression.SyntaxTree)
        {
            semanticModel = null;
        }

        var typeInfo = semanticModel?.GetTypeInfo(expression);
        var typeSymbol = typeInfo?.ConvertedType ?? typeInfo?.Type;

        if (typeSymbol != null &&
            typeSymbol.Name != "ValidateValueCallback" &&
            typeSymbol.ContainingNamespace?.ToDisplayString() != "System.Windows")
        {
            return null;
        }

        var callback = UnwrapDelegate(expression);
        if (callback is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression))
            return null;

        return callback switch
        {
            SimpleLambdaExpressionSyntax simple => NormalizeValidateLambda(simple, propertyType),
            ParenthesizedLambdaExpressionSyntax parenthesized => NormalizeValidateLambda(parenthesized, propertyType),
            IdentifierNameSyntax or MemberAccessExpressionSyntax => callback.WithoutTrivia(),
            _ => CreateValidateLambdaFromExpression(callback, propertyType)
        };
    }

    private static ExpressionSyntax NormalizeValidateLambda(SimpleLambdaExpressionSyntax lambda, TypeSyntax? propertyType)
    {
        if (propertyType == null)
            return lambda;

        if (lambda.Parameter.Type != null)
            return lambda;

        return lambda.WithParameter(lambda.Parameter.WithType(propertyType.WithoutTrivia()));
    }

    private static ExpressionSyntax NormalizeValidateLambda(ParenthesizedLambdaExpressionSyntax lambda, TypeSyntax? propertyType)
    {
        if (propertyType == null)
            return lambda;

        if (lambda.ParameterList.Parameters.Count == 0)
            return lambda;

        var first = lambda.ParameterList.Parameters[0];
        if (first.Type != null)
            return lambda;

        var updatedFirst = first.WithType(propertyType.WithoutTrivia());
        var parameters = lambda.ParameterList.Parameters.Replace(first, updatedFirst);
        return lambda.WithParameterList(lambda.ParameterList.WithParameters(parameters));
    }

    private static ExpressionSyntax CreateValidateLambdaFromExpression(ExpressionSyntax expression, TypeSyntax? propertyType)
    {
        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("value"));
        if (propertyType != null)
        {
            parameter = parameter.WithType(propertyType.WithoutTrivia());
        }

        return SyntaxFactory.SimpleLambdaExpression(
                parameter,
                SyntaxFactory.InvocationExpression(
                    expression.WithoutTrivia(),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value"))))))
            .WithLeadingTrivia(expression.GetLeadingTrivia())
            .WithTrailingTrivia(expression.GetTrailingTrivia());
    }

    private static ExpressionSyntax CreateCoerceLambda(ExpressionSyntax expression, TypeSyntax? propertyType)
    {
        var callback = UnwrapDelegate(expression);

        if (callback is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            return callback.WithoutTrivia();
        }

        var senderParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("sender"))
            .WithType(SyntaxFactory.IdentifierName("AvaloniaObject"));

        var valueParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("value"));
        if (propertyType != null)
        {
            valueParameter = valueParameter.WithType(propertyType.WithoutTrivia());
        }

        var invocation = SyntaxFactory.InvocationExpression(
            callback.WithoutTrivia(),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("sender")),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName("value"))
                })));

        ExpressionSyntax body = propertyType != null
            ? SyntaxFactory.CastExpression(propertyType.WithoutTrivia(), invocation)
            : invocation;

        return SyntaxFactory.ParenthesizedLambdaExpression(body)
            .WithParameterList(
                SyntaxFactory.ParameterList(
                    SyntaxFactory.SeparatedList(new[] { senderParameter, valueParameter })));
    }

    private static ExpressionSyntax UnwrapDelegate(ExpressionSyntax expression)
    {
        if (expression is ObjectCreationExpressionSyntax objectCreation &&
            objectCreation.ArgumentList?.Arguments.Count == 1)
        {
            return objectCreation.ArgumentList.Arguments[0].Expression;
        }

        return expression;
    }

    private static string? GetMethodName(ExpressionSyntax? expression)
    {
        if (expression == null)
            return null;

        var unwrapped = UnwrapDelegate(expression);

        return unwrapped switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            _ => null
        };
    }

    private static SyntaxNode UpdateValidateMethodSignature(
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string methodName,
        TypeSyntax? propertyType)
    {
        if (propertyType == null)
            return root;

        var method = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal));

        if (method == null)
            return root;

        if (method.ParameterList.Parameters.Count != 1)
            return root;

        var parameter = method.ParameterList.Parameters[0];
        var parameterType = propertyType.WithoutTrivia()
            .WithLeadingTrivia(parameter.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(parameter.Type?.GetTrailingTrivia() ?? default);

        var updatedParameter = parameter.WithType(parameterType);
        var updatedMethod = method.WithParameterList(
            method.ParameterList.WithParameters(
                SyntaxFactory.SingletonSeparatedList(updatedParameter)));

        return root.ReplaceNode(method, updatedMethod);
    }

    private static SyntaxNode UpdateCoerceMethodSignature(
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        string methodName,
        TypeSyntax? propertyType)
    {
        if (propertyType == null)
            return root;

        var method = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => string.Equals(m.Identifier.Text, methodName, StringComparison.Ordinal));

        if (method == null)
            return root;

        if (method.ParameterList.Parameters.Count != 2)
            return root;

        var parameters = method.ParameterList.Parameters;
        var ownerParameter = parameters[0];
        var valueParameter = parameters[1];

        var avaloniaObjectType = SyntaxFactory.IdentifierName("AvaloniaObject")
            .WithLeadingTrivia(ownerParameter.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(ownerParameter.Type?.GetTrailingTrivia() ?? default);

        var propertyTypeWithTrivia = propertyType.WithoutTrivia()
            .WithLeadingTrivia(valueParameter.Type?.GetLeadingTrivia() ?? default)
            .WithTrailingTrivia(valueParameter.Type?.GetTrailingTrivia() ?? default);

        var updatedOwnerParameter = ownerParameter.WithType(avaloniaObjectType);
        var updatedValueParameter = valueParameter.WithType(propertyTypeWithTrivia);

        var updatedParameters = SyntaxFactory.SeparatedList(new[] { updatedOwnerParameter, updatedValueParameter });

        var returnType = propertyType.WithoutTrivia()
            .WithLeadingTrivia(method.ReturnType.GetLeadingTrivia())
            .WithTrailingTrivia(method.ReturnType.GetTrailingTrivia());

        var updatedMethod = method
            .WithReturnType(returnType)
            .WithParameterList(method.ParameterList.WithParameters(updatedParameters));

        return root.ReplaceNode(method, updatedMethod);
    }

    private static ClassDeclarationSyntax? FindMatchingClass(SyntaxNode root, ClassDeclarationSyntax originalClass)
    {
        var matchBySpan = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cd => cd.SpanStart == originalClass.SpanStart && cd.Identifier.Text == originalClass.Identifier.Text);

        if (matchBySpan != null)
            return matchBySpan;

        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(cd => cd.Identifier.Text == originalClass.Identifier.Text);
    }

    private static IEnumerable<string> CollectOptionNames(ExpressionSyntax expression, SemanticModel? semanticModel)
    {
        if (semanticModel != null && semanticModel.SyntaxTree != expression.SyntaxTree)
        {
            semanticModel = null;
        }

        var results = new HashSet<string>(StringComparer.Ordinal);
        Collect(expression);
        return results;

        void Collect(ExpressionSyntax expr)
        {
            switch (expr)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    Collect(parenthesized.Expression);
                    return;
                case BinaryExpressionSyntax binary
                    when binary.IsKind(SyntaxKind.BitwiseOrExpression) ||
                         binary.IsKind(SyntaxKind.BitwiseAndExpression):
                    Collect(binary.Left);
                    Collect(binary.Right);
                    return;
            }

            if (semanticModel != null)
            {
                var symbol = semanticModel.GetSymbolInfo(expr).Symbol;
                if (symbol is IFieldSymbol field &&
                    field.ContainingType?.Name == "FrameworkPropertyMetadataOptions")
                {
                    results.Add(field.Name);
                    return;
                }

                var constantValue = semanticModel.GetConstantValue(expr);
                if (constantValue.HasValue && constantValue.Value is int intValue)
                {
                    foreach (var (flag, name) in FrameworkOptionFlags)
                    {
                        if ((intValue & flag) == flag)
                        {
                            results.Add(name);
                        }
                    }

                    return;
                }
            }

            if (expr is MemberAccessExpressionSyntax member)
            {
                results.Add(member.Name.Identifier.Text);
                return;
            }

            if (expr is IdentifierNameSyntax identifier)
            {
                results.Add(identifier.Identifier.Text);
            }
        }
    }

    private static bool IsPropertyChangedCallbackType(ITypeSymbol? typeSymbol) =>
        typeSymbol != null &&
        typeSymbol.Name == "PropertyChangedCallback" &&
        typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Windows";

    private static bool IsFrameworkOptionsType(ITypeSymbol? typeSymbol) =>
        typeSymbol != null &&
        typeSymbol.Name == "FrameworkPropertyMetadataOptions" &&
        typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Windows";

    private static bool IsCoerceValueCallbackType(ITypeSymbol? typeSymbol) =>
        typeSymbol != null &&
        typeSymbol.Name == "CoerceValueCallback" &&
        typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Windows";

    private sealed class MetadataInfo
    {
        public bool HasMetadata { get; set; }
        public ExpressionSyntax? DefaultValue { get; set; }
        public ExpressionSyntax? PropertyChangedCallback { get; set; }
        public bool AffectsMeasure { get; set; }
        public bool AffectsArrange { get; set; }
        public bool AffectsRender { get; set; }
        public bool Inherits { get; set; }
        public bool BindsTwoWayByDefault { get; set; }
        public ExpressionSyntax? CoerceValueCallback { get; set; }

        public bool RequiresBindingModeUsing => BindsTwoWayByDefault;
        public bool HasLayoutStatements => AffectsMeasure || AffectsArrange || AffectsRender;

        public static MetadataInfo Empty { get; } = new();

        public static MetadataInfo Create(ExpressionSyntax? metadataExpression, SemanticModel? semanticModel)
        {
            if (metadataExpression is not ObjectCreationExpressionSyntax objectCreation ||
                objectCreation.ArgumentList == null)
            {
                return Empty;
            }

            if (semanticModel != null && semanticModel.SyntaxTree != metadataExpression.SyntaxTree)
            {
                semanticModel = null;
            }

            var info = new MetadataInfo
            {
                HasMetadata = true
            };

            foreach (var argument in objectCreation.ArgumentList.Arguments)
            {
                var expression = argument.Expression;
                var typeInfo = semanticModel?.GetTypeInfo(expression);
                var convertedType = typeInfo?.ConvertedType ?? typeInfo?.Type;

                if (IsPropertyChangedCallbackType(convertedType))
                {
                    info.PropertyChangedCallback ??= UnwrapDelegate(expression);
                    continue;
                }

                if (IsFrameworkOptionsType(convertedType))
                {
                    ApplyOptions(expression, semanticModel, info);
                    continue;
                }

                if (IsCoerceValueCallbackType(convertedType))
                {
                    info.CoerceValueCallback ??= UnwrapDelegate(expression);
                    continue;
                }

                if (info.DefaultValue == null)
                {
                    info.DefaultValue = expression;
                    continue;
                }

                if (info.PropertyChangedCallback == null &&
                    (expression is IdentifierNameSyntax || expression is MemberAccessExpressionSyntax))
                {
                    info.PropertyChangedCallback = UnwrapDelegate(expression);
                    continue;
                }

                if (info.CoerceValueCallback == null &&
                    (expression is IdentifierNameSyntax || expression is MemberAccessExpressionSyntax))
                {
                    info.CoerceValueCallback = UnwrapDelegate(expression);
                }
            }

            if (info.PropertyChangedCallback is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                info.PropertyChangedCallback = null;
            }

            return info;
        }

        private static void ApplyOptions(ExpressionSyntax expression, SemanticModel? semanticModel, MetadataInfo info)
        {
            foreach (var option in CollectOptionNames(expression, semanticModel))
            {
                switch (option)
                {
                    case "AffectsMeasure":
                        info.AffectsMeasure = true;
                        break;
                    case "AffectsArrange":
                        info.AffectsArrange = true;
                        break;
                    case "AffectsRender":
                        info.AffectsRender = true;
                        break;
                    case "Inherits":
                        info.Inherits = true;
                        break;
                    case "BindsTwoWayByDefault":
                        info.BindsTwoWayByDefault = true;
                        break;
                }
            }
        }
    }

    private static readonly (int Flag, string Name)[] FrameworkOptionFlags =
    {
        (0x001, "AffectsMeasure"),
        (0x002, "AffectsArrange"),
        (0x010, "AffectsRender"),
        (0x020, "Inherits"),
        (0x100, "BindsTwoWayByDefault"),
    };
}
