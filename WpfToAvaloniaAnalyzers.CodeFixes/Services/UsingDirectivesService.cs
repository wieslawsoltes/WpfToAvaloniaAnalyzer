using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

public static class UsingDirectivesService
{
    public static CompilationUnitSyntax RemoveWpfUsings(CompilationUnitSyntax compilationUnit)
    {
        var usingsToKeep = compilationUnit.Usings
            .Where(u =>
            {
                var name = u.Name?.ToString();
                return name != "System.Windows" && name != "System.Windows.Controls";
            })
            .ToList();

        return compilationUnit.WithUsings(SyntaxFactory.List(usingsToKeep));
    }

    public static CompilationUnitSyntax AddAvaloniaUsings(CompilationUnitSyntax compilationUnit)
    {
        var usings = compilationUnit.Usings.ToList();

        var hasAvaloniaUsing = usings.Any(u => u.Name?.ToString() == "Avalonia");
        var hasAvaloniaControlsUsing = usings.Any(u => u.Name?.ToString() == "Avalonia.Controls");

        if (!hasAvaloniaUsing)
        {
            usings.Add(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Avalonia"))
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(SyntaxFactory.LineFeed)));
        }

        if (!hasAvaloniaControlsUsing)
        {
            usings.Add(SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("Avalonia"),
                    SyntaxFactory.IdentifierName("Controls")))
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(SyntaxFactory.LineFeed)));
        }

        return compilationUnit.WithUsings(SyntaxFactory.List(usings));
    }

    public static CompilationUnitSyntax AddAvaloniaInteractivityUsing(CompilationUnitSyntax compilationUnit)
    {
        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == "Avalonia.Interactivity"))
        {
            return compilationUnit;
        }

        var interactivityUsing = SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("Avalonia"),
                    SyntaxFactory.IdentifierName("Interactivity")))
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(SyntaxFactory.LineFeed));

        return compilationUnit.WithUsings(compilationUnit.Usings.Add(interactivityUsing));
    }

    public static CompilationUnitSyntax AddAvaloniaDataUsing(CompilationUnitSyntax compilationUnit)
    {
        var hasAvaloniaDataUsing = compilationUnit.Usings
            .Any(u => u.Name?.ToString() == "Avalonia.Data");

        if (hasAvaloniaDataUsing)
            return compilationUnit;

        var avaloniaDataUsing = SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.IdentifierName("Avalonia"),
                    SyntaxFactory.IdentifierName("Data")))
            .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(SyntaxFactory.LineFeed));

        var updatedUsings = compilationUnit.Usings.Add(avaloniaDataUsing);
        return compilationUnit.WithUsings(updatedUsings);
    }

    public static bool HasWpfUsings(CompilationUnitSyntax compilationUnit)
    {
        return compilationUnit.Usings.Any(u =>
        {
            var name = u.Name?.ToString();
            return name == "System.Windows" || name == "System.Windows.Controls";
        });
    }
}
