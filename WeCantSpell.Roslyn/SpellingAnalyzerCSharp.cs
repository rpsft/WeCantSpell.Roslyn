using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Infrastructure;

namespace WeCantSpell.Roslyn
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [UsedImplicitly]
    [SuppressMessage(
        "ReSharper",
        "ArrangeObjectCreationWhenTypeEvident",
        Justification = "https://github.com/dotnet/roslyn-analyzers/issues/5828"
    )]
    public sealed class SpellingAnalyzerCSharp : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_spellingIdentifierDiagnosticDescriptor =
            new DiagnosticDescriptor(
                "SP3110",
                "Identifier Spelling",
                "Identifier spelling error: {0}",
                "Naming",
                DiagnosticSeverity.Warning,
                true,
                "Identifier name may contain a spelling error."
            );

        private static readonly DiagnosticDescriptor s_spellingLiteralDiagnosticDescriptor = new DiagnosticDescriptor(
            "SP3111",
            "Text Literal Spelling",
            "Text literal spelling error: {0}",
            "Spelling",
            DiagnosticSeverity.Warning,
            true,
            "Text literal may contain a spelling error."
        );

        private static readonly DiagnosticDescriptor s_spellingCommentDiagnosticDescriptor = new DiagnosticDescriptor(
            "SP3112",
            "Comment Spelling",
            "Comment spelling error: {0}",
            "Spelling",
            DiagnosticSeverity.Warning,
            true,
            "Comment may contain a spelling error."
        );

        private static readonly DiagnosticDescriptor s_spellingDocumentationDiagnosticDescriptor =
            new DiagnosticDescriptor(
                "SP3113",
                "Documentation Spelling",
                "Documentation spelling error: {0}",
                "Spelling",
                DiagnosticSeverity.Warning,
                true,
                "Documentation may contain a spelling error."
            );

        private static readonly ImmutableArray<DiagnosticDescriptor> s_supportedDiagnosticsArray =
            ImmutableArray.Create(
                s_spellingIdentifierDiagnosticDescriptor,
                s_spellingLiteralDiagnosticDescriptor,
                s_spellingCommentDiagnosticDescriptor,
                s_spellingDocumentationDiagnosticDescriptor
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => s_supportedDiagnosticsArray;

        internal static ImmutableArray<string> SupportedDiagnosticIds =>
            ImmutableArray.Create(s_supportedDiagnosticsArray.Select(descriptor => descriptor.Id).ToArray());

        private ISpellChecker? InvariantSpellChecker { get; }

        private readonly IFileSystem _fileSystem;

        public SpellingAnalyzerCSharp()
            : this(new FileSystem()) { }

        public SpellingAnalyzerCSharp(ISpellChecker spellChecker)
            : this(spellChecker, new FileSystem()) { }

        public SpellingAnalyzerCSharp(ISpellChecker spellChecker, IFileSystem fileSystem)
            : this(fileSystem)
        {
            InvariantSpellChecker = spellChecker;
        }

        public SpellingAnalyzerCSharp(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(
                // TODO: Make this a config switch
                // GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics
                GeneratedCodeAnalysisFlags.None
            );
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            // Read the AdditionalFiles set once per compilation. Capturing the resulting index in the
            // per-tree action makes the analyzer depend on AdditionalFiles content, so Roslyn re-runs
            // it (with fresh content) whenever a dictionary or config file is edited.
            var dictionaryIndex = AdditionalFilesDictionaryIndex.Build(context.Options.AdditionalFiles);
            context.RegisterSyntaxTreeAction(treeContext => HandleSyntaxTree(treeContext, dictionaryIndex));
        }

        private void HandleSyntaxTree(SyntaxTreeAnalysisContext context, AdditionalFilesDictionaryIndex dictionaryIndex)
        {
            SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            var spellChecker = GetSpellChecker(context.Tree.FilePath, dictionaryIndex, context.CancellationToken);

            var walker = new SpellCheckCSharpWalker(spellChecker, ReportMistakeAsDiagnostic);
            walker.Visit(root);

            void ReportMistakeAsDiagnostic(SpellingMistake mistake)
            {
                ReportDiagnostic(mistake, context);
            }
        }

        private ISpellChecker GetSpellChecker(
            string treeFilePath,
            AdditionalFilesDictionaryIndex dictionaryIndex,
            CancellationToken cancellationToken
        )
        {
            if (InvariantSpellChecker != null)
                return InvariantSpellChecker;

            // Prefer dictionaries declared as AdditionalFiles (reliable cache invalidation).
            var fromAdditionalFiles = dictionaryIndex.GetSpellChecker(treeFilePath, cancellationToken);
            if (fromAdditionalFiles != null)
                return fromAdditionalFiles;

            // Fall back to the legacy on-disk directory walk for consumers that have not declared the
            // dictionaries as AdditionalFiles. This path keeps the previous (build-server-cached) behaviour.
            SpellCheckerOptions checkerOptions = new SpellCheckerOptions(_fileSystem, treeFilePath);
            return SpellCheckerPool.Shared.Get(checkerOptions);
        }

        private static void ReportDiagnostic(SpellingMistake mistake, SyntaxTreeAnalysisContext context)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            DiagnosticDescriptor descriptor = SelectDescriptor(mistake.Kind);
            var diagnostic = Diagnostic.Create(descriptor, mistake.Location, mistake.Text);
            context.ReportDiagnostic(diagnostic);
        }

        private static DiagnosticDescriptor SelectDescriptor(SpellingMistakeKind kind)
        {
            return kind switch
            {
                SpellingMistakeKind.Identifier => s_spellingIdentifierDiagnosticDescriptor,
                SpellingMistakeKind.Literal => s_spellingLiteralDiagnosticDescriptor,
                SpellingMistakeKind.Comment => s_spellingCommentDiagnosticDescriptor,
                SpellingMistakeKind.Documentation => s_spellingDocumentationDiagnosticDescriptor,
                _ => throw new NotSupportedException(),
            };
        }

        static SpellingAnalyzerCSharp()
        {
            EmbeddedDllDependency.Init();
        }
    }
}
