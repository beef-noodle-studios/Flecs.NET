using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NoodleStudios.Flecs.Analyzers;

/// <summary>
///     Adds (or repairs) <c>[StructLayout(LayoutKind.Sequential)]</c> on an aspect
///     type that the analyzer flagged for missing or non-sequential layout.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddStructLayoutCodeFixProvider)), Shared]
public sealed class AddStructLayoutCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add [StructLayout(LayoutKind.Sequential)]";
    private const string InteropNamespace = "System.Runtime.InteropServices";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(AspectAnalyzer.MissingSequentialLayout.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document
            .GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        Diagnostic diagnostic = context.Diagnostics[0];
        TypeDeclarationSyntax? typeDecl = root
            .FindNode(diagnostic.Location.SourceSpan)
            .FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => Task.FromResult(ApplyFix(context.Document, root, typeDecl)),
                equivalenceKey: AspectAnalyzer.MissingSequentialLayout.Id),
            diagnostic);
    }

    private static Document ApplyFix(Document document, SyntaxNode root, TypeDeclarationSyntax typeDecl)
    {
        AttributeSyntax? existing = FindStructLayout(typeDecl);

        TypeDeclarationSyntax newTypeDecl = existing is not null
            ? typeDecl.ReplaceNode(existing, ToSequential(existing))
            : AddSequentialAttribute(typeDecl);

        SyntaxNode newRoot = root.ReplaceNode(typeDecl, newTypeDecl);

        if (newRoot is CompilationUnitSyntax unit && !HasInteropUsing(unit))
            newRoot = AddInteropUsing(unit);

        return document.WithSyntaxRoot(newRoot);
    }

    private static AttributeSyntax? FindStructLayout(TypeDeclarationSyntax typeDecl) =>
        typeDecl.AttributeLists
            .SelectMany(list => list.Attributes)
            .FirstOrDefault(attr =>
            {
                string name = attr.Name.ToString();
                return name is "StructLayout" or "StructLayoutAttribute"
                    || name.EndsWith(".StructLayout", System.StringComparison.Ordinal)
                    || name.EndsWith(".StructLayoutAttribute", System.StringComparison.Ordinal);
            });

    private static AttributeSyntax ToSequential(AttributeSyntax existing)
    {
        ExpressionSyntax sequential = SyntaxFactory.ParseExpression("LayoutKind.Sequential");

        if (existing.ArgumentList is { Arguments.Count: > 0 } args)
        {
            AttributeArgumentSyntax first = args.Arguments[0];
            return existing.ReplaceNode(first, first.WithExpression(sequential));
        }

        return existing.WithArgumentList(SyntaxFactory.AttributeArgumentList(
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(sequential))));
    }

    private static TypeDeclarationSyntax AddSequentialAttribute(TypeDeclarationSyntax typeDecl)
    {
        AttributeSyntax sequential = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("StructLayout"),
            SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression("LayoutKind.Sequential")))));

        SyntaxTriviaList leading = typeDecl.GetLeadingTrivia();
        SyntaxTrivia indent = leading.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));

        SyntaxTriviaList trailing = indent.IsKind(SyntaxKind.WhitespaceTrivia)
            ? SyntaxFactory.TriviaList(SyntaxFactory.LineFeed, indent)
            : SyntaxFactory.TriviaList(SyntaxFactory.LineFeed);

        AttributeListSyntax attrList = SyntaxFactory
            .AttributeList(SyntaxFactory.SingletonSeparatedList(sequential))
            .WithLeadingTrivia(leading)
            .WithTrailingTrivia(trailing);

        return typeDecl
            .WithoutLeadingTrivia()
            .WithAttributeLists(typeDecl.AttributeLists.Insert(0, attrList));
    }

    private static bool HasInteropUsing(CompilationUnitSyntax unit) =>
        unit.Usings.Any(u => u.Name?.ToString() == InteropNamespace);

    private static CompilationUnitSyntax AddInteropUsing(CompilationUnitSyntax unit)
    {
        UsingDirectiveSyntax interop = SyntaxFactory
            .UsingDirective(SyntaxFactory.ParseName(InteropNamespace))
            .NormalizeWhitespace()
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        return unit.AddUsings(interop);
    }
}
