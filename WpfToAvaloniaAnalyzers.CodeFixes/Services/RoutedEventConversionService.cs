using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using WpfToAvaloniaAnalyzers;
using WpfToAvaloniaAnalyzers.CodeFixes.Helpers;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class RoutedEventConversionService
{
    public static SyntaxNode ConvertRoutedEventField(
        SyntaxNode root,
        VariableDeclaratorSyntax fieldVariable,
        SemanticModel semanticModel)
    {
        if (fieldVariable.Parent?.Parent is not FieldDeclarationSyntax fieldDeclaration)
            return root;

        if (fieldVariable.Initializer?.Value is not InvocationExpressionSyntax invocation)
            return root;

        var invocationInfo = semanticModel.GetSymbolInfo(invocation);
        if (invocationInfo.Symbol is not IMethodSymbol methodSymbol)
            return root;

        if (!string.Equals(methodSymbol.Name, "RegisterRoutedEvent", StringComparison.Ordinal))
            return root;

        if (methodSymbol.ContainingType?.Name != "EventManager")
            return root;

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 4)
            return root;

        var nameExpression = arguments[0].Expression;
        var routingExpression = arguments[1].Expression;
        var handlerExpression = arguments[2].Expression;
        var ownerExpression = arguments[3].Expression;

        var handlerType = GetHandlerTypeSymbol(handlerExpression, semanticModel);
        var ownerTypeSyntax = RoutedEventHelper.ExtractType(ownerExpression) ?? RoutedEventHelper.GuessOwnerType(fieldDeclaration);
        var eventArgsType = RoutedEventHelper.GetAvaloniaEventArgsType(handlerType, out _);
        var routingStrategiesExpression = RoutedEventHelper.ConvertRoutingStrategyExpression(routingExpression);

        if (eventArgsType == null || ownerTypeSyntax == null)
            return root;

        var registerInvocation = CreateRegisterInvocation(
            nameExpression,
            routingStrategiesExpression,
            ownerTypeSyntax,
            eventArgsType);

        var newVariable = fieldVariable.WithInitializer(SyntaxFactory.EqualsValueClause(registerInvocation))
            .WithTriviaFrom(fieldVariable);
        var newDeclaration = fieldDeclaration.Declaration
            .WithType(RoutedEventHelper.CreateRoutedEventType(eventArgsType))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable))
            .WithTriviaFrom(fieldDeclaration.Declaration);

        if (newDeclaration.Type != null)
        {
            newDeclaration = newDeclaration.WithType(newDeclaration.Type.WithTrailingTrivia(SyntaxFactory.Space));
        }

        var updatedField = fieldDeclaration.WithDeclaration(newDeclaration)
            .WithTriviaFrom(fieldDeclaration);

        var normalizedField = updatedField.NormalizeWhitespace()
            .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia())
            .WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia());

        return root.ReplaceNode(fieldDeclaration, normalizedField);
    }

    public static SyntaxNode ConvertRoutedEventAccessor(
        SyntaxNode root,
        EventDeclarationSyntax eventDeclaration,
        SemanticModel semanticModel)
    {
        var eventTypeInfo = semanticModel.GetTypeInfo(eventDeclaration.Type);
        if (eventTypeInfo.Type is not INamedTypeSymbol eventTypeSymbol)
            return root;

        var eventArgsType = RoutedEventHelper.GetAvaloniaEventArgsType(eventTypeSymbol, out var originalEventArgsSymbol);
        if (eventArgsType == null)
            return root;

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>
        {
            [eventDeclaration] = eventDeclaration.WithType(
                RoutedEventHelper.CreateEventHandlerType(eventArgsType).WithTriviaFrom(eventDeclaration.Type))
        };

        if (originalEventArgsSymbol != null &&
            eventDeclaration.Parent is ClassDeclarationSyntax classDeclaration)
        {
            foreach (var method in classDeclaration.Members.OfType<MethodDeclarationSyntax>())
            {
                var updatedMethod = UpdateHandlerMethodParameters(method, originalEventArgsSymbol, eventArgsType, semanticModel);
                updatedMethod = ApplyRoutedEventSignature(updatedMethod, eventArgsType);
                if (!ReferenceEquals(method, updatedMethod))
                {
                    replacements[method] = updatedMethod;
                }
                else
                {
                    var fallbackMethod = ApplyRoutedEventSignature(method, eventArgsType);
                    if (!ReferenceEquals(method, fallbackMethod))
                    {
                        replacements[method] = fallbackMethod;
                    }
                }
            }
        }

        var updatedRoot = replacements.Count == 0
            ? root
            : root.ReplaceNodes(replacements.Keys, (original, _) => replacements[original].WithTriviaFrom(original));

        return NormalizeRoutedEventHandlerSenders(updatedRoot);
    }

    public static SyntaxNode ConvertRoutedEventAddOwner(
        SyntaxNode root,
        VariableDeclaratorSyntax fieldVariable,
        InvocationExpressionSyntax invocation)
    {
        if (fieldVariable.Parent?.Parent is not FieldDeclarationSyntax fieldDeclaration)
            return root;

        if (invocation.ArgumentList.Arguments.Count != 1)
            return root;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return root;

        var newInitializerExpression = memberAccess.Expression.WithoutTrivia();

        var newVariable = fieldVariable.WithInitializer(SyntaxFactory.EqualsValueClause(newInitializerExpression))
            .WithTriviaFrom(fieldVariable);

        var newType = SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.RoutedEvent")
            .WithTriviaFrom(fieldDeclaration.Declaration.Type);

        newType = newType.WithTrailingTrivia(SyntaxFactory.Space);

        var newDeclaration = fieldDeclaration.Declaration
            .WithType(newType)
            .WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable))
            .WithTriviaFrom(fieldDeclaration.Declaration);

        if (newDeclaration.Type != null)
        {
            newDeclaration = newDeclaration.WithType(newDeclaration.Type.WithTrailingTrivia(SyntaxFactory.Space));
        }

        var updatedField = fieldDeclaration.WithDeclaration(newDeclaration)
            .WithTriviaFrom(fieldDeclaration);

        var normalizedField = updatedField.NormalizeWhitespace()
            .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia())
            .WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia());

        return root.ReplaceNode(fieldDeclaration, normalizedField);
    }

    public static SyntaxNode ConvertRegisterClassHandler(
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count < 3)
            return root;

        var targetTypeExpression = arguments[0].Expression;
        var routedEventExpression = arguments[1].Expression;
        var handlerExpression = arguments[2].Expression;
        var handledEventsTooExpression = arguments.Count >= 4 ? arguments[3].Expression : null;

        var handlerDelegate = RoutedEventSyntaxHelper.CreateClassHandlerDelegate(handlerExpression);
        if (handlerDelegate == null)
            return root;

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        var handlerMethods = GetHandlerMethodSymbols(handlerExpression, semanticModel);
        var processedHandlerMethods = new HashSet<MethodDeclarationSyntax>();
        var handlerProcessingQueue = new List<(MethodDeclarationSyntax MethodSyntax, ITypeSymbol? EventArgsSymbol, string? ParameterTypeText)>();

        foreach (var handlerMethod in handlerMethods)
        {
            if (handlerMethod.Parameters.Length < 2)
                continue;

            foreach (var syntaxReference in handlerMethod.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax methodDeclaration)
                    continue;

                if (root.FindNode(methodDeclaration.Span) is not MethodDeclarationSyntax rootMethodDeclaration)
                    continue;

                if (!processedHandlerMethods.Add(rootMethodDeclaration))
                    continue;

                var originalEventArgsSymbol = handlerMethod.OriginalDefinition.Parameters.Length > 1
                    ? handlerMethod.OriginalDefinition.Parameters[1].Type
                    : handlerMethod.Parameters[1].Type;

                string? parameterTypeText = null;

                if (methodDeclaration.ParameterList.Parameters.Count > 1 &&
                    methodDeclaration.ParameterList.Parameters[1].Type is TypeSyntax parameterTypeSyntax)
                {
                    var parameterTypeInfo = semanticModel.GetTypeInfo(parameterTypeSyntax);
                    originalEventArgsSymbol = parameterTypeInfo.Type ?? parameterTypeInfo.ConvertedType ?? originalEventArgsSymbol;
                    parameterTypeText = parameterTypeSyntax.ToString();
                }

                handlerProcessingQueue.Add((rootMethodDeclaration, originalEventArgsSymbol, parameterTypeText));
            }
        }

        if (handlerProcessingQueue.Count == 0)
        {
            foreach (var methodSyntax in FindHandlerMethodDeclarations(root, handlerExpression))
            {
                if (!processedHandlerMethods.Add(methodSyntax))
                    continue;

                string? parameterTypeText = null;
                if (methodSyntax.ParameterList.Parameters.Count > 1 && methodSyntax.ParameterList.Parameters[1].Type != null)
                {
                    parameterTypeText = methodSyntax.ParameterList.Parameters[1].Type!.ToString();
                }

                handlerProcessingQueue.Add((methodSyntax, null, parameterTypeText));
            }
        }

        foreach (var (methodSyntax, originalEventArgsSymbol, parameterTypeText) in handlerProcessingQueue)
        {
            var mappedEventArgsType = originalEventArgsSymbol != null
                ? RoutedEventHelper.MapEventArgsType(originalEventArgsSymbol, parameterTypeText)
                : RoutedEventHelper.MapEventArgsTypeFromText(parameterTypeText) ?? SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.RoutedEventArgs");

            var updatedMethod = UpdateHandlerMethodParameters(
                methodSyntax,
                originalEventArgsSymbol,
                mappedEventArgsType,
                semanticModel,
                parameterTypeText);

            updatedMethod = ApplyRoutedEventSignature(updatedMethod, mappedEventArgsType);
            updatedMethod = EnsureEventArgsParameterType(updatedMethod, mappedEventArgsType);

            if (!ReferenceEquals(methodSyntax, updatedMethod))
            {
                replacements[methodSyntax] = updatedMethod;
            }
            else
            {
                var fallbackMethod = ApplyRoutedEventSignature(methodSyntax, mappedEventArgsType);
                if (!ReferenceEquals(methodSyntax, fallbackMethod))
                {
                    replacements[methodSyntax] = fallbackMethod;
                }
            }
        }

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            routedEventExpression.WithoutTrivia(),
            SyntaxFactory.IdentifierName("AddClassHandler"));

        var invocationArguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(targetTypeExpression.WithoutTrivia()),
            SyntaxFactory.Argument(handlerDelegate)
        };

        if (handledEventsTooExpression != null)
        {
            invocationArguments.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("handledEventsToo"),
                    default,
                    handledEventsTooExpression.WithoutTrivia()));
        }

        var newInvocation = SyntaxFactory.InvocationExpression(
            memberAccess,
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(invocationArguments)))
            .WithTriviaFrom(invocation);

        var workingRoot = root;
        if (replacements.Count > 0)
        {
            workingRoot = workingRoot.ReplaceNodes(
                replacements.Keys,
                (original, _) => replacements[original].WithTriviaFrom(original));
        }

        var currentInvocation = workingRoot.GetCurrentNode(invocation) ?? workingRoot.FindNode(invocation.Span) as InvocationExpressionSyntax;
        if (currentInvocation == null)
            return workingRoot;

        workingRoot = workingRoot.ReplaceNode(currentInvocation, newInvocation);
        return NormalizeRoutedEventHandlerSenders(workingRoot);
    }

    public static SyntaxNode ConvertInstanceHandlerInvocation(
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count < 2)
            return root;

        var routedEventArgument = invocation.ArgumentList.Arguments[0];
        var handlerArgument = invocation.ArgumentList.Arguments[1];
        var normalizedHandler = RoutedEventSyntaxHelper.NormalizeHandlerExpression(handlerArgument.Expression);
        if (normalizedHandler == null)
            return root;

        var updatedRoutedEventArgument = routedEventArgument.WithExpression(routedEventArgument.Expression.WithoutTrivia())
            .WithTriviaFrom(routedEventArgument);

        var updatedArguments = new List<ArgumentSyntax>
        {
            updatedRoutedEventArgument
        };

        var updatedHandlerArgument = SyntaxFactory.Argument(normalizedHandler)
            .WithTriviaFrom(handlerArgument);
        updatedArguments.Add(updatedHandlerArgument);

        if (invocation.ArgumentList.Arguments.Count >= 3)
        {
            var handledEventsToo = invocation.ArgumentList.Arguments[2];
            var handledEventsArgument = SyntaxFactory.Argument(
                    SyntaxFactory.NameColon("handledEventsToo"),
                    default,
                    handledEventsToo.Expression.WithoutTrivia())
                .WithTriviaFrom(handledEventsToo);
            updatedArguments.Add(handledEventsArgument);
        }

        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();

        var handlerSymbolSet = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var symbol in GetHandlerMethodSymbols(handlerArgument.Expression, semanticModel))
        {
            if (symbol != null)
            {
                handlerSymbolSet.Add(symbol);
            }
        }

        var processedInstanceMethods = new HashSet<MethodDeclarationSyntax>();
        var instanceProcessingQueue = new List<(MethodDeclarationSyntax MethodSyntax, ITypeSymbol? EventArgsSymbol, string? ParameterTypeText)>();

        foreach (var handlerSymbol in handlerSymbolSet)
        {
            if (handlerSymbol.Parameters.Length < 2)
                continue;

            foreach (var syntaxReference in handlerSymbol.DeclaringSyntaxReferences)
            {
                if (root.FindNode(syntaxReference.Span) is not MethodDeclarationSyntax methodDeclaration)
                    continue;

                if (!processedInstanceMethods.Add(methodDeclaration))
                    continue;

                var originalEventArgsSymbol = handlerSymbol.OriginalDefinition.Parameters.Length > 1
                    ? handlerSymbol.OriginalDefinition.Parameters[1].Type
                    : handlerSymbol.Parameters[1].Type;

                string? parameterTypeText = null;

                if (methodDeclaration.ParameterList.Parameters.Count > 1 &&
                    methodDeclaration.ParameterList.Parameters[1].Type is TypeSyntax parameterTypeSyntax)
                {
                    var parameterTypeInfo = semanticModel.GetTypeInfo(parameterTypeSyntax);
                    originalEventArgsSymbol = parameterTypeInfo.Type ?? parameterTypeInfo.ConvertedType ?? originalEventArgsSymbol;
                    parameterTypeText = parameterTypeSyntax.ToString();
                }

                instanceProcessingQueue.Add((methodDeclaration, originalEventArgsSymbol, parameterTypeText));
            }
        }

        if (instanceProcessingQueue.Count == 0)
        {
            foreach (var methodSyntax in FindHandlerMethodDeclarations(root, handlerArgument.Expression))
            {
                if (!processedInstanceMethods.Add(methodSyntax))
                    continue;

                string? parameterTypeText = null;
                if (methodSyntax.ParameterList.Parameters.Count > 1 && methodSyntax.ParameterList.Parameters[1].Type != null)
                {
                    parameterTypeText = methodSyntax.ParameterList.Parameters[1].Type!.ToString();
                }

                instanceProcessingQueue.Add((methodSyntax, null, parameterTypeText));
            }
        }

        foreach (var (methodDeclaration, originalEventArgsSymbol, parameterTypeText) in instanceProcessingQueue)
        {
            var mappedEventArgsType = originalEventArgsSymbol != null
                ? RoutedEventHelper.MapEventArgsType(originalEventArgsSymbol, parameterTypeText)
                : RoutedEventHelper.MapEventArgsTypeFromText(parameterTypeText) ?? SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.RoutedEventArgs");

            var updatedMethod = UpdateHandlerMethodParameters(
                methodDeclaration,
                originalEventArgsSymbol,
                mappedEventArgsType,
                semanticModel,
                parameterTypeText);

            updatedMethod = ApplyRoutedEventSignature(updatedMethod, mappedEventArgsType);
            updatedMethod = EnsureEventArgsParameterType(updatedMethod, mappedEventArgsType);

            if (!ReferenceEquals(methodDeclaration, updatedMethod))
            {
                replacements[methodDeclaration] = updatedMethod;
            }
            else
            {
                var fallbackMethod = ApplyRoutedEventSignature(methodDeclaration, mappedEventArgsType);
                if (!ReferenceEquals(methodDeclaration, fallbackMethod))
                {
                    replacements[methodDeclaration] = fallbackMethod;
                }
            }
        }

        var newInvocation = invocation.WithArgumentList(
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(updatedArguments)))
            .WithTriviaFrom(invocation);

        var workingRoot = root;
        if (replacements.Count > 0)
        {
            workingRoot = workingRoot.ReplaceNodes(
                replacements.Keys,
                (original, _) => replacements[original].WithTriviaFrom(original));
        }

        var currentInvocation = workingRoot.GetCurrentNode(invocation) ?? workingRoot.FindNode(invocation.Span) as InvocationExpressionSyntax;
        if (currentInvocation == null)
            return workingRoot;

        workingRoot = workingRoot.ReplaceNode(currentInvocation, newInvocation);
        return NormalizeRoutedEventHandlerSenders(workingRoot);
    }

    public static SyntaxNode ConvertRaiseEventInvocation(
        SyntaxNode root,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
            return root;

        var argument = invocation.ArgumentList.Arguments[0];
        if (argument.Expression is not ObjectCreationExpressionSyntax objectCreation)
            return root;

        if (objectCreation.Type is not IdentifierNameSyntax && objectCreation.Type is not QualifiedNameSyntax)
            return root;

        var newType = SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.RoutedEventArgs")
            .WithTriviaFrom(objectCreation.Type);

        var updatedObjectCreation = objectCreation.WithType(newType);
        if (objectCreation.ArgumentList == null)
        {
            updatedObjectCreation = updatedObjectCreation.WithArgumentList(SyntaxFactory.ArgumentList());
        }

        var updatedInvocation = invocation.ReplaceNode(objectCreation, updatedObjectCreation);

        if (invocation.Parent is ConditionalAccessExpressionSyntax conditionalParent)
        {
            var updatedConditional = conditionalParent;
            var updatedTarget = RewriteRaiseEventTarget(conditionalParent.Expression);
            if (!ReferenceEquals(updatedTarget, conditionalParent.Expression))
            {
                updatedConditional = updatedConditional.WithExpression(updatedTarget);
            }

            var invocationWithTrivia = updatedInvocation.WithTriviaFrom(invocation);
            if (!ReferenceEquals(invocationWithTrivia, conditionalParent.WhenNotNull))
            {
                updatedConditional = updatedConditional.WithWhenNotNull(invocationWithTrivia);
            }

            return root.ReplaceNode(conditionalParent, updatedConditional.WithTriviaFrom(conditionalParent));
        }

        var updatedExpression = RewriteRaiseEventTarget(invocation.Expression);
        if (!ReferenceEquals(updatedExpression, invocation.Expression))
        {
            updatedInvocation = updatedInvocation.WithExpression(updatedExpression);
        }

        return root.ReplaceNode(invocation, updatedInvocation.WithTriviaFrom(invocation));
    }

    private static ExpressionSyntax RewriteRaiseEventTarget(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized when parenthesized.Expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AsExpression):
                if (binary.Right is TypeSyntax rightType)
                {
                    var mapped = MapRaiseEventTargetType(rightType);
                    if (mapped != null)
                    {
                        var updatedBinary = binary.WithRight(mapped.WithTriviaFrom(rightType));
                        return parenthesized.WithExpression(updatedBinary);
                    }
                }

                break;
            case CastExpressionSyntax cast:
            {
                var mapped = MapRaiseEventTargetType(cast.Type);
                if (mapped != null)
                {
                    return cast.WithType(mapped.WithTriviaFrom(cast.Type));
                }

                break;
            }
            case MemberAccessExpressionSyntax memberAccess:
            {
                var updated = RewriteRaiseEventTarget(memberAccess.Expression);
                if (!ReferenceEquals(updated, memberAccess.Expression))
                {
                    return memberAccess.WithExpression(updated);
                }

                break;
            }
        }

        return expression;
    }

    private static TypeSyntax? MapRaiseEventTargetType(TypeSyntax type)
    {
        var token = type.ToString();
        return token switch
        {
            "FrameworkElement" or "System.Windows.FrameworkElement" or
            "UIElement" or "System.Windows.UIElement" or
            "DependencyObject" or "System.Windows.DependencyObject" or
            "FrameworkContentElement" or "System.Windows.FrameworkContentElement" => SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.Interactive").WithTrailingTrivia(SyntaxFactory.Space),
            _ => null
        };
    }

    private static TypeSyntax CreateRoutedEventType(TypeSyntax eventArgsType)
    {
        var eventArgsText = eventArgsType.WithoutTrivia().ToFullString();
        return SyntaxFactory.ParseTypeName($"global::Avalonia.Interactivity.RoutedEvent<{eventArgsText}>")
            .WithTrailingTrivia(SyntaxFactory.Space);
    }

    private static InvocationExpressionSyntax CreateRegisterInvocation(
        ExpressionSyntax nameExpression,
        ExpressionSyntax routingStrategiesExpression,
        TypeSyntax ownerType,
        TypeSyntax eventArgsType)
    {
        var routedEventIdentifier = SyntaxFactory.ParseExpression("global::Avalonia.Interactivity.RoutedEvent");

        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            routedEventIdentifier,
            SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Register"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        ownerType.WithoutTrivia(),
                        eventArgsType.WithoutTrivia()
                    }))));

        return SyntaxFactory.InvocationExpression(
            memberAccess,
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(nameExpression.WithoutTrivia()),
                    SyntaxFactory.Argument(routingStrategiesExpression.WithoutTrivia())
                })));
    }

    private static TypeSyntax CreateEventHandlerType(TypeSyntax eventArgsType)
    {
        var eventArgsText = eventArgsType.WithoutTrivia().ToFullString();
        return SyntaxFactory.ParseTypeName($"global::System.EventHandler<{eventArgsText}>")
            .WithTrailingTrivia(SyntaxFactory.Space);
    }

    private static MethodDeclarationSyntax UpdateHandlerMethodParameters(
        MethodDeclarationSyntax method,
        ITypeSymbol? originalEventArgsSymbol,
        TypeSyntax updatedEventArgsType,
        SemanticModel semanticModel,
        string? originalEventArgsText = null)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count == 0)
            return method;

        var updatedParameters = new List<ParameterSyntax>();
        var changed = false;

        for (var index = 0; index < parameters.Count; index++)
        {
            var parameter = parameters[index];
            if (parameter.Type == null)
            {
                updatedParameters.Add(parameter);
                continue;
            }

            var parameterTypeInfo = semanticModel.GetTypeInfo(parameter.Type);
            var parameterType = parameterTypeInfo.Type ?? parameterTypeInfo.ConvertedType;
            var parameterTypeText = parameter.Type.ToString();

            if (index == 0)
            {
                var mappedSenderType = MapSenderParameterType(parameterType, parameter.Type, originalEventArgsSymbol);
                if (mappedSenderType != null)
                {
                    var replacementType = mappedSenderType.WithTriviaFrom(parameter.Type);
                    if (!SyntaxFactory.AreEquivalent(parameter.Type, replacementType))
                    {
                        updatedParameters.Add(parameter.WithType(replacementType));
                        changed = true;
                        continue;
                    }
                }
            }

            if (index == 1)
            {
                var mappedFromText = RoutedEventHelper.MapEventArgsTypeFromText(parameterTypeText);
                if (mappedFromText != null)
                {
                    var replacementType = mappedFromText.WithTriviaFrom(parameter.Type);
                    if (!SyntaxFactory.AreEquivalent(parameter.Type, replacementType))
                    {
                        updatedParameters.Add(parameter.WithType(replacementType));
                        changed = true;
                        continue;
                    }
                }
            }

            if (IsEventArgsParameterMatch(parameterType, parameter.Type, originalEventArgsSymbol, index, originalEventArgsText))
            {
                var replacementType = updatedEventArgsType.WithTriviaFrom(parameter.Type);
                if (!SyntaxFactory.AreEquivalent(parameter.Type, replacementType))
                {
                    updatedParameters.Add(parameter.WithType(replacementType));
                    changed = true;
                    continue;
                }
            }

            updatedParameters.Add(parameter);
        }

        if (!changed)
            return method;

        return method.WithParameterList(
            method.ParameterList.WithParameters(SyntaxFactory.SeparatedList(updatedParameters)));
    }

    private static MethodDeclarationSyntax EnsureEventArgsParameterType(MethodDeclarationSyntax method, TypeSyntax eventArgsType)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count > 1 && parameters[1].Type != null)
        {
            var currentTypeText = parameters[1].Type!.ToString();
            var eventArgsText = eventArgsType.ToString();
            var eventArgsIsGeneric = eventArgsText.IndexOf("Avalonia.Interactivity.RoutedEventArgs", StringComparison.Ordinal) >= 0;
            var currentIsInput = currentTypeText.IndexOf("Avalonia.Input.", StringComparison.Ordinal) >= 0;

            if (!(eventArgsIsGeneric && currentIsInput))
            {
                var desiredType = eventArgsType.WithTriviaFrom(parameters[1].Type!);
                if (!SyntaxFactory.AreEquivalent(parameters[1].Type, desiredType))
                {
                    method = method.ReplaceNode(parameters[1].Type!, desiredType);
                }
            }
        }

        return method;
    }

    private static bool IsEventArgsParameterMatch(
        ITypeSymbol? parameterTypeSymbol,
        TypeSyntax parameterTypeSyntax,
        ITypeSymbol? originalEventArgsSymbol,
        int parameterIndex,
        string? originalEventArgsText)
    {
        if (originalEventArgsSymbol == null)
            return parameterIndex == 1;

        if (parameterTypeSymbol != null &&
            SymbolEqualityComparer.Default.Equals(parameterTypeSymbol, originalEventArgsSymbol))
        {
            return true;
        }

        var parameterTypeText = parameterTypeSyntax.ToString();
        var fullyQualifiedOriginal = originalEventArgsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (string.Equals(parameterTypeText, fullyQualifiedOriginal, StringComparison.Ordinal) ||
            string.Equals(parameterTypeText, originalEventArgsSymbol.ToDisplayString(), StringComparison.Ordinal) ||
            string.Equals(parameterTypeText, originalEventArgsSymbol.Name, StringComparison.Ordinal) ||
            (parameterIndex == 1 && string.Equals(parameterTypeText, originalEventArgsSymbol.Name, StringComparison.Ordinal)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(originalEventArgsText))
        {
            var normalizedParameter = NormalizeTypeName(parameterTypeSyntax.ToString());
            var normalizedOriginal = NormalizeTypeName(originalEventArgsText);
            if (string.Equals(normalizedParameter, normalizedOriginal, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static TypeSyntax? MapSenderParameterType(ITypeSymbol? senderType, TypeSyntax parameterTypeSyntax, ITypeSymbol? originalEventArgsSymbol)
    {
        var mappedFromSymbol = MapSenderParameterTypeFromSymbol(senderType, originalEventArgsSymbol);
        if (mappedFromSymbol != null)
            return mappedFromSymbol;

        return MapSenderParameterTypeFromText(parameterTypeSyntax.ToString(), originalEventArgsSymbol);
    }

    private static TypeSyntax? MapSenderParameterTypeFromSymbol(ITypeSymbol? senderType, ITypeSymbol? originalEventArgsSymbol)
    {
        if (senderType == null)
            return null;

        if (IsInputEventArgs(originalEventArgsSymbol))
        {
            return SyntaxFactory.ParseTypeName("object").WithTrailingTrivia(SyntaxFactory.Space);
        }

        var displayName = senderType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var prefersAvaloniaObject = IsPropertyChangedEventArgs(originalEventArgsSymbol);
        var prefersInteractive = IsRoutedEventArgs(originalEventArgsSymbol) || !prefersAvaloniaObject;

        if (displayName == "global::System.Windows.Controls.Control")
            return SyntaxFactory.ParseTypeName("global::Avalonia.Controls.Control").WithTrailingTrivia(SyntaxFactory.Space);

        if (displayName == "global::System.Windows.Window")
            return SyntaxFactory.ParseTypeName("global::Avalonia.Controls.Window").WithTrailingTrivia(SyntaxFactory.Space);

        if (prefersAvaloniaObject && IsDependencyLikeType(displayName))
        {
            return SyntaxFactory.ParseTypeName("global::Avalonia.AvaloniaObject").WithTrailingTrivia(SyntaxFactory.Space);
        }

        if (prefersInteractive && IsDependencyLikeType(displayName))
        {
            return SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.Interactive").WithTrailingTrivia(SyntaxFactory.Space);
        }

        return null;
    }

    private static TypeSyntax? MapSenderParameterTypeFromText(string typeText, ITypeSymbol? originalEventArgsSymbol)
    {
        if (IsInputEventArgs(originalEventArgsSymbol) || IsInputEventArgs(typeText))
        {
            return SyntaxFactory.ParseTypeName("object").WithTrailingTrivia(SyntaxFactory.Space);
        }

        var normalized = NormalizeTypeName(typeText);
        var prefersAvaloniaObject = IsPropertyChangedEventArgs(originalEventArgsSymbol);
        var prefersInteractive = IsRoutedEventArgs(originalEventArgsSymbol) || !prefersAvaloniaObject;

        if (normalized == "global::System.Windows.Controls.Control")
            return SyntaxFactory.ParseTypeName("global::Avalonia.Controls.Control").WithTrailingTrivia(SyntaxFactory.Space);

        if (normalized == "global::System.Windows.Window")
            return SyntaxFactory.ParseTypeName("global::Avalonia.Controls.Window").WithTrailingTrivia(SyntaxFactory.Space);

        if (IsDependencyLikeType(normalized))
        {
            if (prefersAvaloniaObject)
            {
                return SyntaxFactory.ParseTypeName("global::Avalonia.AvaloniaObject").WithTrailingTrivia(SyntaxFactory.Space);
            }

            if (prefersInteractive)
            {
                return SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.Interactive").WithTrailingTrivia(SyntaxFactory.Space);
            }
        }

        return null;
    }

    private static bool IsDependencyLikeType(string displayName) => displayName switch
    {
        "global::System.Windows.DependencyObject" => true,
        "global::System.Windows.FrameworkElement" => true,
        "global::System.Windows.UIElement" => true,
        "global::System.Windows.FrameworkContentElement" => true,
        _ => false
    };

    private static bool IsPropertyChangedEventArgs(ITypeSymbol? eventArgsSymbol)
    {
        if (eventArgsSymbol == null)
            return false;

        var displayName = eventArgsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (displayName == "global::System.Windows.DependencyPropertyChangedEventArgs" ||
            displayName.StartsWith("global::Avalonia.AvaloniaPropertyChangedEventArgs", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(eventArgsSymbol.Name, "DependencyPropertyChangedEventArgs", StringComparison.Ordinal);
    }

    private static bool IsRoutedEventArgs(ITypeSymbol? eventArgsSymbol)
    {
        if (eventArgsSymbol == null)
            return false;

        var displayName = eventArgsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (displayName == "global::System.Windows.RoutedEventArgs" ||
            displayName.StartsWith("global::System.Windows.Input.", StringComparison.Ordinal))
        {
            return true;
        }

        if (displayName == "global::Avalonia.Interactivity.RoutedEventArgs" ||
            displayName.StartsWith("global::Avalonia.Input.", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(eventArgsSymbol.Name, "RoutedEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "MouseEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "MouseButtonEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "KeyEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "TextCompositionEventArgs", StringComparison.Ordinal);
    }

    private static bool IsInputEventArgs(ITypeSymbol? eventArgsSymbol)
    {
        if (eventArgsSymbol == null)
            return false;

        var displayName = eventArgsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (displayName.IndexOf("global::System.Windows.Input.", StringComparison.Ordinal) >= 0 ||
            displayName.IndexOf("global::Avalonia.Input.", StringComparison.Ordinal) >= 0)
        {
            return true;
        }

        return string.Equals(eventArgsSymbol.Name, "MouseEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "MouseButtonEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "PointerEventArgs", StringComparison.Ordinal) ||
               string.Equals(eventArgsSymbol.Name, "PointerPressedEventArgs", StringComparison.Ordinal);
    }

    private static bool IsInputEventArgs(string typeText)
    {
        var normalized = NormalizeTypeName(typeText);

        if (normalized.IndexOf("global::System.Windows.Input.", StringComparison.Ordinal) >= 0 ||
            normalized.IndexOf("global::Avalonia.Input.", StringComparison.Ordinal) >= 0)
        {
            return true;
        }

        return normalized.EndsWith("MouseEventArgs", StringComparison.Ordinal) ||
               normalized.EndsWith("MouseButtonEventArgs", StringComparison.Ordinal) ||
               normalized.EndsWith("PointerEventArgs", StringComparison.Ordinal) ||
               normalized.EndsWith("PointerPressedEventArgs", StringComparison.Ordinal);
    }

    private static string NormalizeTypeName(string typeText)
    {
        var trimmed = typeText.Trim();

        if (trimmed.Length == 0)
            return trimmed;

        if (trimmed.StartsWith("global::", StringComparison.Ordinal))
            return trimmed;

        if (trimmed.StartsWith("System.", StringComparison.Ordinal) ||
            trimmed.StartsWith("Avalonia.", StringComparison.Ordinal))
        {
            return $"global::{trimmed}";
        }

        return $"global::System.Windows.{trimmed}";
    }

    private static MethodDeclarationSyntax ApplyRoutedEventSignature(MethodDeclarationSyntax method, TypeSyntax eventArgsType)
    {
        var parameters = method.ParameterList.Parameters;
        if (parameters.Count == 0)
            return method;

        var rewritten = method;

        var desiredSenderType = GetDefaultSenderType(eventArgsType);
        if (parameters.Count > 1 && desiredSenderType != null && parameters[0].Type != null)
        {
            var normalizedSender = NormalizeTypeName(parameters[0].Type!.ToString());
            if (IsPotentialWpfSenderType(normalizedSender))
            {
                rewritten = rewritten.ReplaceNode(parameters[0].Type!, desiredSenderType.WithTriviaFrom(parameters[0].Type!));
            }
        }

        parameters = rewritten.ParameterList.Parameters;
        if (parameters.Count > 1 && parameters[1].Type != null)
        {
            var currentTypeText = parameters[1].Type!.ToString();
            var eventArgsText = eventArgsType.ToString();

            var eventArgsIsGeneric = eventArgsText.IndexOf("Avalonia.Interactivity.RoutedEventArgs", StringComparison.Ordinal) >= 0;
            var currentIsInput = currentTypeText.IndexOf("Avalonia.Input.", StringComparison.Ordinal) >= 0;

            if (!(eventArgsIsGeneric && currentIsInput))
            {
                rewritten = rewritten.ReplaceNode(parameters[1].Type!, eventArgsType.WithTriviaFrom(parameters[1].Type!));
            }
        }

        return rewritten;
    }

    private static TypeSyntax? GetDefaultSenderType(TypeSyntax eventArgsType)
    {
        var eventArgsText = eventArgsType.ToString();
        if (eventArgsText.IndexOf("AvaloniaPropertyChangedEventArgs", StringComparison.Ordinal) >= 0)
        {
            return SyntaxFactory.ParseTypeName("global::Avalonia.AvaloniaObject").WithTrailingTrivia(SyntaxFactory.Space);
        }

        if (eventArgsText.IndexOf("Avalonia.Input.", StringComparison.Ordinal) >= 0)
        {
            return SyntaxFactory.ParseTypeName("object").WithTrailingTrivia(SyntaxFactory.Space);
        }

        if (string.Equals(eventArgsText, "global::Avalonia.Interactivity.RoutedEventArgs", StringComparison.Ordinal) ||
            string.Equals(eventArgsText, "Avalonia.Interactivity.RoutedEventArgs", StringComparison.Ordinal))
        {
            return SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.Interactive").WithTrailingTrivia(SyntaxFactory.Space);
        }

        return null;
    }

    private static SyntaxNode NormalizeRoutedEventHandlerSenders(SyntaxNode root)
    {
        return root.ReplaceNodes(
            root.DescendantNodes().OfType<MethodDeclarationSyntax>(),
            (original, _) =>
            {
                var parameters = original.ParameterList.Parameters;
                if (parameters.Count < 2)
                    return original;

                var firstType = parameters[0].Type;
                var secondType = parameters[1].Type;
                if (firstType == null || secondType == null)
                    return original;

                var secondText = secondType.ToString();
                if (!IsAvaloniaEventArgsType(secondText))
                    return original;

                var normalizedSender = NormalizeTypeName(firstType.ToString());
                if (!IsPotentialWpfSenderType(normalizedSender))
                    return original;

                TypeSyntax desiredSender;
                if (secondText.IndexOf("AvaloniaPropertyChangedEventArgs", StringComparison.Ordinal) >= 0)
                {
                    desiredSender = SyntaxFactory.ParseTypeName("global::Avalonia.AvaloniaObject");
                }
                else if (secondText.IndexOf("Avalonia.Input.", StringComparison.Ordinal) >= 0)
                {
                    desiredSender = SyntaxFactory.ParseTypeName("object");
                }
                else
                {
                    desiredSender = SyntaxFactory.ParseTypeName("global::Avalonia.Interactivity.Interactive");
                }

                var updated = original.ReplaceNode(firstType, desiredSender.WithTriviaFrom(firstType));
                updated = updated.ReplaceNode(secondType, SyntaxFactory.ParseTypeName(secondText).WithTriviaFrom(secondType));
                return updated;
            });
    }

    private static bool IsAvaloniaEventArgsType(string typeText) =>
        typeText.IndexOf("Avalonia.Interactivity.RoutedEventArgs", StringComparison.Ordinal) >= 0 ||
        typeText.IndexOf("Avalonia.Input.", StringComparison.Ordinal) >= 0 ||
        typeText.IndexOf("Avalonia.AvaloniaPropertyChangedEventArgs", StringComparison.Ordinal) >= 0;

    private static bool IsPotentialWpfSenderType(string normalizedType)
    {
        if (normalizedType == "global::System.Windows.Controls.Control" ||
            normalizedType == "global::System.Windows.Window")
        {
            return true;
        }

        return IsDependencyLikeType(normalizedType);
    }

    private static ImmutableArray<IMethodSymbol> GetHandlerMethodSymbols(ExpressionSyntax handlerExpression, SemanticModel semanticModel)
    {
        var builder = ImmutableArray.CreateBuilder<IMethodSymbol>();

        switch (handlerExpression)
        {
            case ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList != null:
                foreach (var argument in objectCreation.ArgumentList.Arguments)
                {
                    AddMethodSymbols(semanticModel.GetSymbolInfo(argument.Expression), builder);
                }
                break;

            case IdentifierNameSyntax or MemberAccessExpressionSyntax:
            {
                AddMethodSymbols(semanticModel.GetSymbolInfo(handlerExpression), builder);
                break;
            }

            case ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax:
                {
                    AddMethodSymbols(semanticModel.GetSymbolInfo(handlerExpression), builder);
                    break;
                }
        }

        return builder.ToImmutable();
    }

    private static void AddMethodSymbols(SymbolInfo symbolInfo, ImmutableArray<IMethodSymbol>.Builder builder)
    {
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.DeclaringSyntaxReferences.Length > 0)
        {
            builder.Add(methodSymbol);
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod && candidateMethod.DeclaringSyntaxReferences.Length > 0)
            {
                builder.Add(candidateMethod);
            }
        }
    }

    private static ITypeSymbol? GetHandlerTypeSymbol(ExpressionSyntax handlerExpression, SemanticModel semanticModel)
    {
        if (handlerExpression is TypeOfExpressionSyntax typeOfExpression)
        {
            var typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type);
            return typeInfo.Type;
        }

        var symbol = semanticModel.GetSymbolInfo(handlerExpression).Symbol;
        return symbol as ITypeSymbol;
    }

    private static IEnumerable<MethodDeclarationSyntax> FindHandlerMethodDeclarations(SyntaxNode root, ExpressionSyntax handlerExpression)
    {
        var methodNames = new HashSet<string>(StringComparer.Ordinal);

        switch (handlerExpression)
        {
            case IdentifierNameSyntax identifier:
                methodNames.Add(identifier.Identifier.ValueText);
                break;
            case MemberAccessExpressionSyntax memberAccess:
                methodNames.Add(memberAccess.Name.Identifier.ValueText);
                break;
            case ObjectCreationExpressionSyntax objectCreation:
                if (objectCreation.ArgumentList?.Arguments.Count > 0)
                {
                    var firstArgument = objectCreation.ArgumentList.Arguments[0].Expression;
                    if (firstArgument is IdentifierNameSyntax ctorIdentifier)
                    {
                        methodNames.Add(ctorIdentifier.Identifier.ValueText);
                    }
                    else if (firstArgument is MemberAccessExpressionSyntax ctorMemberAccess)
                    {
                        methodNames.Add(ctorMemberAccess.Name.Identifier.ValueText);
                    }
                }
                break;
        }

        if (methodNames.Count == 0)
            yield break;

        var returned = new HashSet<MethodDeclarationSyntax>();
        var containingType = handlerExpression.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingType != null)
        {
            foreach (var method in containingType.Members.OfType<MethodDeclarationSyntax>())
            {
                if (methodNames.Contains(method.Identifier.ValueText) && returned.Add(method))
                {
                    yield return method;
                }
            }
        }

        foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (methodNames.Contains(method.Identifier.ValueText) && returned.Add(method))
            {
                yield return method;
            }
        }
    }
}
