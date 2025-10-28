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

    public static TypeSyntax MapEventArgsType(ITypeSymbol eventArgsSymbol, string? originalTypeText = null)
    {
        if (eventArgsSymbol is IErrorTypeSymbol && !string.IsNullOrWhiteSpace(originalTypeText))
        {
            var fromText = MapEventArgsTypeFromText(originalTypeText);
            if (fromText != null)
            {
                return fromText;
            }
        }

        if (!string.IsNullOrWhiteSpace(originalTypeText))
        {
            var normalizedOriginal = NormalizeTypeName(originalTypeText);
            if (normalizedOriginal.IndexOf("Avalonia.", StringComparison.Ordinal) >= 0)
            {
                return SyntaxFactory.ParseTypeName(normalizedOriginal);
            }

            foreach (var candidate in EnumerateCandidateTypeNames(originalTypeText))
            {
                if (TryMapEventArgs(candidate, out var mappedCandidate))
                {
                    return SyntaxFactory.ParseTypeName(mappedCandidate);
                }
            }

            var fromText = MapEventArgsTypeFromText(originalTypeText);
            if (fromText != null)
            {
                return fromText;
            }
        }

        var fullName = eventArgsSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (fullName.StartsWith("global::Avalonia.", StringComparison.Ordinal))
        {
            return SyntaxFactory.ParseTypeName(fullName);
        }

        if (TryMapEventArgs(fullName, out var mapped))
        {
            return SyntaxFactory.ParseTypeName(mapped);
        }

        if (string.Equals(eventArgsSymbol.Name, "RoutedEventArgs", StringComparison.Ordinal))
        {
            return SyntaxFactory.ParseTypeName(DefaultAvaloniaEventArgsType);
        }

        return SyntaxFactory.ParseTypeName(DefaultAvaloniaEventArgsType);
    }

    public static TypeSyntax? MapEventArgsTypeFromText(string? typeText)
    {
        if (string.IsNullOrWhiteSpace(typeText))
            return null;

        foreach (var candidate in EnumerateCandidateTypeNames(typeText))
        {
            if (TryMapEventArgs(candidate, out var mappedCandidate))
            {
                return SyntaxFactory.ParseTypeName(mappedCandidate);
            }
        }

        var normalized = NormalizeTypeName(typeText);
        if (normalized.IndexOf("Avalonia.", StringComparison.Ordinal) >= 0)
        {
            return SyntaxFactory.ParseTypeName(normalized);
        }

        return null;
    }

    private static bool TryMapEventArgs(string typeName, out string mapped)
    {
        if (s_eventArgsMap.TryGetValue(typeName, out mapped))
        {
            return true;
        }

        const string globalPrefix = "global::";
        if (typeName.StartsWith(globalPrefix, StringComparison.Ordinal))
        {
            var trimmed = typeName.Substring(globalPrefix.Length);
            if (s_eventArgsMap.TryGetValue(trimmed, out mapped))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> EnumerateCandidateTypeNames(string originalTypeText)
    {
        var trimmed = originalTypeText.Trim();
        if (trimmed.Length == 0)
            yield break;

        yield return trimmed;

        const string globalPrefix = "global::";
        var withoutGlobal = trimmed.StartsWith(globalPrefix, StringComparison.Ordinal)
            ? trimmed.Substring(globalPrefix.Length)
            : trimmed;

        if (!string.Equals(withoutGlobal, trimmed, StringComparison.Ordinal))
        {
            yield return withoutGlobal;
        }

        if (!trimmed.StartsWith(globalPrefix, StringComparison.Ordinal))
        {
            yield return $"{globalPrefix}{trimmed}";
        }

        if (!withoutGlobal.StartsWith(globalPrefix, StringComparison.Ordinal))
        {
            yield return $"{globalPrefix}{withoutGlobal}";
        }

        var simpleName = withoutGlobal;
        var lastDot = withoutGlobal.LastIndexOf('.');
        if (lastDot >= 0 && lastDot + 1 < withoutGlobal.Length)
        {
            simpleName = withoutGlobal.Substring(lastDot + 1);
        }

        if (!string.Equals(simpleName, withoutGlobal, StringComparison.Ordinal))
        {
            yield return simpleName;
        }

        yield return $"System.Windows.{simpleName}";
        yield return $"{globalPrefix}System.Windows.{simpleName}";
        yield return $"System.Windows.Input.{simpleName}";
        yield return $"{globalPrefix}System.Windows.Input.{simpleName}";
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
}
