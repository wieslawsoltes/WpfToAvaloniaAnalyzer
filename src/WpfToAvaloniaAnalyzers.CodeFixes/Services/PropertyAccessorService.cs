using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class PropertyAccessorService
{
    public static SyntaxNode RemoveCastsFromGetValue(SyntaxNode root)
    {
        var castExpressions = root.DescendantNodes()
            .OfType<CastExpressionSyntax>()
            .Where(cast => cast.Expression is InvocationExpressionSyntax invocation &&
                          invocation.Expression is IdentifierNameSyntax id &&
                          id.Identifier.Text == "GetValue")
            .ToList();

        if (!castExpressions.Any())
            return root;

        return root.ReplaceNodes(castExpressions, (oldNode, newNode) => oldNode.Expression);
    }

    public static bool HasCastsOnGetValue(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<CastExpressionSyntax>()
            .Any(cast => cast.Expression is InvocationExpressionSyntax invocation &&
                        invocation.Expression is IdentifierNameSyntax id &&
                        id.Identifier.Text == "GetValue");
    }
}
