using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class BaseClassService
{
    public static SyntaxNode UpdateWpfBaseClassToAvalonia(SyntaxNode root)
    {
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration?.BaseList == null)
            return root;

        var newBaseTypes = new List<BaseTypeSyntax>();
        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            bool isWpfControl = false;

            // Check for simple identifier (Control)
            if (baseType.Type is IdentifierNameSyntax identifier && identifier.Identifier.Text == "Control")
            {
                isWpfControl = true;
            }
            // Check for fully qualified name (System.Windows.Controls.Control)
            else if (baseType.Type is QualifiedNameSyntax qualifiedName)
            {
                var fullName = qualifiedName.ToString();
                if (fullName == "System.Windows.Controls.Control")
                {
                    isWpfControl = true;
                }
            }

            if (isWpfControl)
            {
                // Replace with Avalonia.Controls.Control
                var avaloniaControl = SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("Avalonia"),
                        SyntaxFactory.IdentifierName("Controls")),
                    SyntaxFactory.IdentifierName("Control"));
                newBaseTypes.Add(SyntaxFactory.SimpleBaseType(avaloniaControl));
            }
            else
            {
                newBaseTypes.Add(baseType);
            }
        }

        var newBaseList = SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(newBaseTypes))
            .WithColonToken(classDeclaration.BaseList.ColonToken);

        var newClassDeclaration = classDeclaration.WithBaseList(newBaseList);
        return root.ReplaceNode(classDeclaration, newClassDeclaration);
    }

    public static bool HasWpfBaseClass(SyntaxNode root)
    {
        var classDeclaration = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault();

        if (classDeclaration?.BaseList == null)
            return false;

        return classDeclaration.BaseList.Types.Any(baseType =>
            baseType.Type is IdentifierNameSyntax identifier && identifier.Identifier.Text == "Control");
    }
}
