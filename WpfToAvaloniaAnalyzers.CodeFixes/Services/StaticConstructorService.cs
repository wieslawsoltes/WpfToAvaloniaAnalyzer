using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WpfToAvaloniaAnalyzers.CodeFixes.Services;

internal static class StaticConstructorService
{
    public static SyntaxNode AddStaticConstructorStatements(
        SyntaxNode root,
        ClassDeclarationSyntax classDeclaration,
        IEnumerable<StatementSyntax> statements)
    {
        var statementsList = statements.ToList();
        if (statementsList.Count == 0)
            return root;

        var staticConstructor = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .FirstOrDefault(ctor => ctor.Modifiers.Any(SyntaxKind.StaticKeyword) && ctor.ParameterList.Parameters.Count == 0);

        if (staticConstructor != null)
        {
            var body = staticConstructor.Body ??
                (staticConstructor.ExpressionBody != null
                    ? SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(staticConstructor.ExpressionBody.Expression))
                    : SyntaxFactory.Block());

            var existingStatements = body.Statements;
            var statementsToInsert = statementsList
                .Where(statement => !existingStatements.Any(existing => existing.IsEquivalentTo(statement)))
                .ToList();

            if (statementsToInsert.Count == 0)
                return root;

            for (var i = statementsToInsert.Count - 1; i >= 0; i--)
            {
                existingStatements = existingStatements.Insert(0, statementsToInsert[i]);
            }

            var newBody = body.WithStatements(existingStatements);

            var updatedConstructor = staticConstructor.WithBody(newBody)
                .WithExpressionBody(null)
                .WithSemicolonToken(default);

            var newMembers = classDeclaration.Members.Replace(staticConstructor, updatedConstructor);
            var updatedClass = classDeclaration.WithMembers(newMembers);

            return root.ReplaceNode(classDeclaration, updatedClass);
        }
        else
        {
            var block = SyntaxFactory.Block(statementsList);
            var constructor = SyntaxFactory.ConstructorDeclaration(classDeclaration.Identifier)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .WithBody(block);

            var newMembers = classDeclaration.Members.Insert(0, constructor);
            var updatedClass = classDeclaration.WithMembers(newMembers);

            return root.ReplaceNode(classDeclaration, updatedClass);
        }
    }
}
