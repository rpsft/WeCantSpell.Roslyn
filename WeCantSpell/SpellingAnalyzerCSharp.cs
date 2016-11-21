﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Utilities;

namespace WeCantSpell
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SpellingAnalyzerCSharp : DiagnosticAnalyzer
    {
        public SpellingAnalyzerCSharp()
        {
        }

        public SpellingAnalyzerCSharp(ISpellChecker spellChecker)
        {
            SpellChecker = spellChecker;
        }

        private static DiagnosticDescriptor SpellingIdentifierDiagnosticDescriptor = new DiagnosticDescriptor(
            "SP3110",
            "Identifier Spelling",
            "Identifier spelling mistake: {0}",
            "Naming",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Identifier name may contain a spelling mistake.");

        private static ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsArray = ImmutableArray.Create(SpellingIdentifierDiagnosticDescriptor);

        public ISpellChecker SpellChecker { get; private set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsArray;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(ClassDeclarationHandler, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(VariableDeclaratorHandler, SyntaxKind.VariableDeclarator);
            context.RegisterSyntaxNodeAction(MethodDeclarationHandler, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(ParameterHandler, SyntaxKind.Parameter);

            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.AnonymousObjectMemberDeclarator);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.BracketedParameterList);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.CatchClause);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.CatchDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.DelegateDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.EnumDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.EnumMemberDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.EventDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.EventFieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.ExternAliasDirective);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.FromClause);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.GenericName);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.JoinClause);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.JoinIntoClause);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.LetClause);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.NamespaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.StructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.UsingDirective);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.UsingStatement);

            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.IdentifierName);

            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.DefineDirectiveTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.DisabledTextTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.InterpolatedStringExpression);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.List);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.MultiLineCommentTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.MultiLineDocumentationCommentTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.PragmaWarningDirectiveTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.RegionDirectiveTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.SingleLineCommentTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.SingleLineDocumentationCommentTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.StringLiteralExpression);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.WarningDirectiveTrivia);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.XmlText);
            context.RegisterSyntaxNodeAction(AnalyzerNotImplemented, SyntaxKind.XmlTextAttribute);
        }

        private void AnalyzerNotImplemented(SyntaxNodeAnalysisContext context)
        {
            var filePath = context.Node.SyntaxTree.FilePath;
            var node = context.Node;
        }

        private void ClassDeclarationHandler(SyntaxNodeAnalysisContext context)
        {
            var node = (ClassDeclarationSyntax)context.Node;
            context.ReportDiagnostics(GenerateSpellingDiagnosticsForIdentifier(node.Identifier));
        }

        private void VariableDeclaratorHandler(SyntaxNodeAnalysisContext context)
        {
            var node = (VariableDeclaratorSyntax)context.Node;
            context.ReportDiagnostics(GenerateSpellingDiagnosticsForIdentifier(node.Identifier));
        }

        private void MethodDeclarationHandler(SyntaxNodeAnalysisContext context)
        {
            var node = (MethodDeclarationSyntax)context.Node;
            context.ReportDiagnostics(GenerateSpellingDiagnosticsForIdentifier(node.Identifier));
        }

        private void ParameterHandler(SyntaxNodeAnalysisContext context)
        {
            var node = (ParameterSyntax)context.Node;
            context.ReportDiagnostics(GenerateSpellingDiagnosticsForIdentifier(node.Identifier));
        }

        private IEnumerable<Diagnostic> GenerateSpellingDiagnosticsForIdentifier(SyntaxToken identifier)
        {
            var wordParser = new IdentifierWordParser();
            var parts = wordParser.SplitWordParts(identifier.Text);

            foreach (var part in parts.Where(part => part.IsWord))
            {
                if (!SpellChecker.Check(part.Text))
                {
                    var spellingStart = identifier.SpanStart + part.Start;

                    var location = Location.Create(
                        identifier.SyntaxTree,
                        TextSpan.FromBounds(
                            spellingStart,
                            spellingStart + part.Length));
                    yield return Diagnostic.Create(SpellingIdentifierDiagnosticDescriptor, location, part.Text);
                }
            }
        }
    }
}