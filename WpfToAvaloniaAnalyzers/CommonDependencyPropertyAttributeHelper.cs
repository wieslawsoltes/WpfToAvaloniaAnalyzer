using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers;

public static class CommonDependencyPropertyAttributeHelper
{
    private const string AttributeTypeName = "CommonDependencyPropertyAttribute";
    private const string AttributeTypeNamespacePrefix = "MS.Internal.";

    public static bool MatchesAttributeType(INamedTypeSymbol? attributeType)
    {
        if (attributeType == null)
            return false;

        if (!string.Equals(attributeType.Name, AttributeTypeName, StringComparison.Ordinal))
            return false;

        var ns = attributeType.ContainingNamespace?.ToDisplayString();
        return ns != null && ns.StartsWith(AttributeTypeNamespacePrefix, StringComparison.Ordinal);
    }

    public static bool IsCommonDependencyPropertyAttribute(AttributeSyntax attributeSyntax, SemanticModel? semanticModel)
    {
        if (semanticModel != null && semanticModel.SyntaxTree == attributeSyntax.SyntaxTree)
        {
            var symbol = semanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
            if (MatchesAttributeType(symbol?.ContainingType))
            {
                return true;
            }
        }

        var identifier = attributeSyntax.Name.ToString();
        return identifier.IndexOf("CommonDependencyProperty", StringComparison.Ordinal) >= 0;
    }
}
