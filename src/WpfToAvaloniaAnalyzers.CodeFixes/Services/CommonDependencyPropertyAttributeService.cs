using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

internal static class CommonDependencyPropertyAttributeService
{
    public static SyntaxNode RemoveCommonDependencyPropertyAttributes(SyntaxNode root, SemanticModel? semanticModel)
    {
        var attributes = root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(attribute => CommonDependencyPropertyAttributeHelper.IsCommonDependencyPropertyAttribute(attribute, semanticModel))
            .ToList();

        if (attributes.Count == 0)
            return root;

        var attributeSet = new HashSet<AttributeSyntax>(attributes);
        var listsToRemove = new HashSet<AttributeListSyntax>();
        var replacements = new Dictionary<AttributeListSyntax, AttributeListSyntax>();

        foreach (var attribute in attributes)
        {
            if (attribute.Parent is not AttributeListSyntax list)
                continue;

            if (list.Attributes.All(attr => attributeSet.Contains(attr)))
            {
                listsToRemove.Add(list);
            }
            else if (list.Attributes.Any(attr => attributeSet.Contains(attr)))
            {
                var remainingAttributes = list.Attributes
                    .Where(attr => !attributeSet.Contains(attr))
                    .ToList();

                var separated = SyntaxFactory.SeparatedList(remainingAttributes);
                replacements[list] = list.WithAttributes(separated);
            }
        }

        var currentRoot = root;

        if (replacements.Count > 0)
        {
            currentRoot = currentRoot.ReplaceNodes(
                replacements.Keys,
                (original, _) => replacements[original]);
        }

        if (listsToRemove.Count > 0)
        {
            currentRoot = currentRoot.RemoveNodes(listsToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
        }

        return currentRoot;
    }

    public static SyntaxNode RemoveAttribute(SyntaxNode root, AttributeSyntax attributeSyntax)
    {
        if (attributeSyntax.Parent is AttributeListSyntax attributeList)
        {
            if (attributeList.Attributes.Count == 1)
            {
                return root.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia)!;
            }

            var remaining = attributeList.Attributes.Remove(attributeSyntax);
            var updatedList = attributeList.WithAttributes(remaining);
            return root.ReplaceNode(attributeList, updatedList);
        }

        return root.RemoveNode(attributeSyntax, SyntaxRemoveOptions.KeepNoTrivia)!;
    }
}
