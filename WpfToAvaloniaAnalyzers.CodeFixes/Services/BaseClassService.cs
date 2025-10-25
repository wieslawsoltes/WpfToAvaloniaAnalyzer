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
            .FirstOrDefault(HasWpfBaseClass);

        return classDeclaration == null
            ? root
            : UpdateWpfBaseClassToAvalonia(root, classDeclaration);
    }

    public static SyntaxNode UpdateWpfBaseClassToAvalonia(SyntaxNode root, ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.BaseList == null)
            return root;

        var newBaseTypes = new List<BaseTypeSyntax>();
        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            if (IsWpfControlBase(baseType.Type))
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

        if (!newBaseTypes.Any())
            return root;

        var newBaseList = SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(newBaseTypes))
            .WithColonToken(classDeclaration.BaseList.ColonToken);

        var newClassDeclaration = classDeclaration.WithBaseList(newBaseList);
        return root.ReplaceNode(classDeclaration, newClassDeclaration);
    }

    public static bool HasWpfBaseClass(SyntaxNode root) =>
        root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Any(HasWpfBaseClass);

    public static bool HasWpfBaseClass(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList != null &&
        classDeclaration.BaseList.Types.Any(baseType => IsWpfControlBase(baseType.Type));

    private static bool IsWpfControlBase(TypeSyntax typeSyntax)
    {
        // Check for simple identifier (Control)
        if (typeSyntax is IdentifierNameSyntax identifier && identifier.Identifier.Text == "Control")
        {
            return true;
        }

        // Check for fully qualified name (System.Windows.Controls.Control)
        if (typeSyntax is QualifiedNameSyntax qualifiedName)
        {
            var fullName = qualifiedName.ToString();
            return fullName == "System.Windows.Controls.Control";
        }

        // Handle alias qualified names: global::System.Windows.Controls.Control
        if (typeSyntax is AliasQualifiedNameSyntax aliasQualified)
        {
            return aliasQualified.ToString() == "global::System.Windows.Controls.Control";
        }

        return false;
    }
}
