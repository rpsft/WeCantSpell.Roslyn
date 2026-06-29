using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Infrastructure;

namespace WeCantSpell.Roslyn
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SpellingCodeFixProviderCSharp))]
    [UsedImplicitly]
    public class SpellingCodeFixProviderCSharp : CodeFixProvider
    {
        private readonly IFileSystem _fileSystem;

        public SpellingCodeFixProviderCSharp()
            : this(new FileSystem()) { }

        public SpellingCodeFixProviderCSharp(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxTree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);

            // Get the SourceText from the syntax tree
            if (syntaxTree == null)
                throw new InvalidOperationException("Can't get document syntax tree");

            var sourceText = await syntaxTree.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            // Extract the text from the diagnostic's SourceSpan
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var diagnosticText = sourceText.GetSubText(diagnosticSpan).ToString();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Add '{diagnosticText}' to project dictionary",
                    createChangedSolution: ct => AddToDictionaryFixAsync(context.Document, diagnosticText, ct),
                    equivalenceKey: nameof(SpellingCodeFixProviderCSharp)
                ),
                diagnostic
            );
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private async Task<Solution> AddToDictionaryFixAsync(
            Document document,
            string dictionaryWord,
            CancellationToken cancellationToken
        )
        {
            if (document.FilePath == null)
                throw new InvalidOperationException("Document has no FilePath, no project dictionary can be found");

            var options = new SpellCheckerOptions(_fileSystem, document.FilePath);
            var dictionaryPath = options.AdditionalDictionaryPaths.OrderByDescending(p => p.Length).FirstOrDefault();
            var solution = document.Project.Solution;

            var additionalDoc =
                dictionaryPath == null
                    ? null
                    : document.Project.AdditionalDocuments.FirstOrDefault(ad =>
                        string.Equals(ad.FilePath, dictionaryPath, StringComparison.OrdinalIgnoreCase)
                    );

            if (additionalDoc != null)
            {
                // The dictionary is a tracked AdditionalFile. Update ONLY the workspace document: the
                // IDE host applies the change (clearing the diagnostic in-process within ~1 s) and
                // persists it to disk itself. Writing the file here as well registers as a competing
                // external change that Rider reconciles only on focus, masking the in-process update
                // and leaving the squiggle for ~30 s.
                var currentText = await additionalDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (!ContainsWord(currentText, dictionaryWord))
                {
                    var suffix = EndsWithNewLine(currentText) ? dictionaryWord + "\n" : "\n" + dictionaryWord + "\n";
                    var newText = currentText.WithChanges(new TextChange(new TextSpan(currentText.Length, 0), suffix));
                    solution = solution.WithAdditionalDocumentText(additionalDoc.Id, newText);
                }

                return solution;
            }

            // Fallback: the dictionary is not tracked as an AdditionalFile (a consumer without the
            // packaged props, or a non-workspace host). Persist directly to disk.
            var updater = SpellCheckerPool.Shared.GetUpdater(options);
            await updater.AddToLocalDictionaryAsync(dictionaryWord).ConfigureAwait(false);
            return solution;
        }

        private static bool ContainsWord(SourceText text, string dictionaryWord)
        {
            foreach (var line in text.Lines)
            {
                if (line.ToString().Trim() == dictionaryWord)
                    return true;
            }

            return false;
        }

        private static bool EndsWithNewLine(SourceText text)
        {
            if (text.Length == 0)
                return true; // empty file: no separator needed before the first word

            var last = text[text.Length - 1];
            return last == '\n' || last == '\r';
        }

        public override ImmutableArray<string> FixableDiagnosticIds => SpellingAnalyzerCSharp.SupportedDiagnosticIds;
    }
}
