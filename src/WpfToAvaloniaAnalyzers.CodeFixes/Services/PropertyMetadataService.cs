using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class PropertyMetadataService
{
    public static SyntaxNode ConvertPropertyMetadataToAvaloniaCallback(SyntaxNode root)
    {
        var propertyMetadataNodes = root.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Where(oc => oc.Type is IdentifierNameSyntax id && id.Identifier.Text == "PropertyMetadata")
            .ToList();

        if (!propertyMetadataNodes.Any())
            return root;

        // For now, we'll focus on the conversion logic in DependencyPropertyService
        // This service is primarily for detecting and providing information about metadata
        return root;
    }

    public static bool HasPropertyChangedCallback(ObjectCreationExpressionSyntax propertyMetadata)
    {
        if (propertyMetadata.ArgumentList?.Arguments.Count >= 2)
        {
            var secondArg = propertyMetadata.ArgumentList.Arguments[1].Expression;
            return secondArg is IdentifierNameSyntax ||
                   secondArg is SimpleLambdaExpressionSyntax ||
                   secondArg is ParenthesizedLambdaExpressionSyntax;
        }
        return false;
    }

    public static ExpressionSyntax? GetPropertyChangedCallback(ObjectCreationExpressionSyntax propertyMetadata)
    {
        if (propertyMetadata.ArgumentList?.Arguments.Count >= 2)
        {
            return propertyMetadata.ArgumentList.Arguments[1].Expression;
        }
        return null;
    }
}
