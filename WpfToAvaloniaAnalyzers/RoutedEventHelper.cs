using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers;

public static class RoutedEventHelper
{
    private static readonly ImmutableDictionary<string, string> s_eventArgsMap =
        ImmutableDictionary.CreateRange(new[]
        {
            new KeyValuePair<string, string>("System.Windows.RoutedEventArgs", "global::Avalonia.Interactivity.RoutedEventArgs"),
            new KeyValuePair<string, string>("System.Windows.Input.KeyEventArgs", "global::Avalonia.Input.KeyEventArgs"),
            new KeyValuePair<string, string>("System.Windows.Input.MouseEventArgs", "global::Avalonia.Input.PointerEventArgs"),
            new KeyValuePair<string, string>("System.Windows.Input.MouseButtonEventArgs", "global::Avalonia.Input.PointerPressedEventArgs"),
            new KeyValuePair<string, string>("System.Windows.Input.TextCompositionEventArgs", "global::Avalonia.Input.TextInputEventArgs")
        });

    private const string DefaultAvaloniaEventArgsType = "global::Avalonia.Interactivity.RoutedEventArgs";

    public static bool IsWpfRoutedEvent(ITypeSymbol typeSymbol)
    {
        return typeSymbol.Name == "RoutedEvent" &&
               typeSymbol.ContainingNamespace?.ToDisplayString() == "System.Windows";
    }

    public static bool IsEventManagerRegisterRoutedEvent(IMethodSymbol methodSymbol)
    {
        if (!string.Equals(methodSymbol.Name, "RegisterRoutedEvent", StringComparison.Ordinal))
            return false;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return false;

        return string.Equals(containingType.Name, "EventManager", StringComparison.Ordinal) &&
               string.Equals(containingType.ContainingNamespace?.ToDisplayString(), "System.Windows", StringComparison.Ordinal);
    }

    public static TypeSyntax GetAvaloniaEventArgsType(ITypeSymbol? handlerType, out ITypeSymbol? originalEventArgsSymbol)
    {
        originalEventArgsSymbol = null;

        if (handlerType is INamedTypeSymbol delegateType && delegateType.DelegateInvokeMethod != null)
        {
            var invokeMethod = delegateType.DelegateInvokeMethod;
            if (invokeMethod.Parameters.Length >= 2)
            {
                originalEventArgsSymbol = invokeMethod.Parameters[1].Type;
                return MapEventArgsType(originalEventArgsSymbol);
            }
        }

        return SyntaxFactory.ParseTypeName(DefaultAvaloniaEventArgsType);
    }

    public static TypeSyntax MapEventArgsType(ITypeSymbol eventArgsSymbol)
    {
        var fullName = eventArgsSymbol.ToDisplayString();
        if (s_eventArgsMap.TryGetValue(fullName, out var mapped))
        {
            return SyntaxFactory.ParseTypeName(mapped);
        }

        if (string.Equals(eventArgsSymbol.Name, "RoutedEventArgs", StringComparison.Ordinal))
        {
            return SyntaxFactory.ParseTypeName(DefaultAvaloniaEventArgsType);
        }

        return SyntaxFactory.ParseTypeName(DefaultAvaloniaEventArgsType);
    }

    public static ExpressionSyntax ConvertRoutingStrategyExpression(ExpressionSyntax expression)
    {
        var text = expression.ToString();

        text = Regex.Replace(text, @"\bSystem\.Windows\.RoutingStrategy\b", "global::Avalonia.Interactivity.RoutingStrategies");
        text = Regex.Replace(text, @"\bSystem\.Windows\.RoutingStrategies\b", "global::Avalonia.Interactivity.RoutingStrategies");
        text = Regex.Replace(text, @"(?<!Avalonia\.Interactivity\.)\bRoutingStrategy\b", "global::Avalonia.Interactivity.RoutingStrategies");
        text = Regex.Replace(text, @"(?<!Avalonia\.Interactivity\.)\bRoutingStrategies\b", "global::Avalonia.Interactivity.RoutingStrategies");

        return SyntaxFactory.ParseExpression(text);
    }

    public static TypeSyntax CreateRoutedEventType(TypeSyntax eventArgsType)
    {
        var eventArgsText = eventArgsType.WithoutTrivia().ToFullString();
        return SyntaxFactory.ParseTypeName($"global::Avalonia.Interactivity.RoutedEvent<{eventArgsText}>")
            .WithTrailingTrivia(SyntaxFactory.Space);
    }

    public static TypeSyntax CreateEventHandlerType(TypeSyntax eventArgsType)
    {
        var eventArgsText = eventArgsType.WithoutTrivia().ToFullString();
        return SyntaxFactory.ParseTypeName($"global::System.EventHandler<{eventArgsText}>")
            .WithTrailingTrivia(SyntaxFactory.Space);
    }

    public static TypeSyntax? ExtractType(ExpressionSyntax expression)
    {
        return expression switch
        {
            TypeOfExpressionSyntax typeOfExpression => typeOfExpression.Type,
            TypeSyntax typeSyntax => typeSyntax,
            _ => null
        };
    }

    public static TypeSyntax? GuessOwnerType(FieldDeclarationSyntax fieldDeclaration)
    {
        if (fieldDeclaration.Parent is ClassDeclarationSyntax classDeclaration)
        {
            return SyntaxFactory.IdentifierName(classDeclaration.Identifier);
        }

        return null;
    }
}
