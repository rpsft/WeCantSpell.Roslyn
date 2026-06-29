using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp
{
    [TestCategory("CSharp")]
    public abstract class CSharpTestBase
    {
        private static readonly string s_pathBase = $"{typeof(CSharpTestBase).Namespace}";
        private static readonly string s_projectNameSingleFileSample = nameof(s_projectNameSingleFileSample);

        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(
            typeof(object).GetTypeInfo().Assembly.Location
        );
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(
            typeof(Enumerable).GetTypeInfo().Assembly.Location
        );
        private static readonly MetadataReference s_cSharpSymbolsReference = MetadataReference.CreateFromFile(
            typeof(CSharpCompilation).GetTypeInfo().Assembly.Location
        );
        private static readonly MetadataReference s_codeAnalysisReference = MetadataReference.CreateFromFile(
            typeof(Compilation).GetTypeInfo().Assembly.Location
        );

        private static Stream OpenCodeFileStream(string embeddedResourceFileName) =>
            typeof(CSharpTestBase)
                .GetTypeInfo()
                .Assembly.GetManifestResourceStream(s_pathBase + "." + embeddedResourceFileName)!;

        private static async Task<string> ReadCodeFileAsStringAsync(string embeddedResourceFileName)
        {
            await using var stream = OpenCodeFileStream(embeddedResourceFileName);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return await reader.ReadToEndAsync();
        }

        protected abstract string CreateResourceNameFromFileName(string fileName);

        private async Task<TextAndVersion> ReadCodeFileAsSTextAndVersionAsync(string fileName) =>
            TextAndVersion.Create(
                SourceText.From(await ReadCodeFileAsStringAsync(CreateResourceNameFromFileName(fileName))),
                VersionStamp.Default,
                fileName
            );

        protected async Task<Project> ReadCodeFileAsProjectAsync(string embeddedResourceFileName) =>
            CreateProjectWithFiles(new[] { await ReadCodeFileAsSTextAndVersionAsync(embeddedResourceFileName) });

        private static Project CreateProjectWithFiles(IEnumerable<TextAndVersion> files)
        {
            var projectId = ProjectId.CreateNewId(debugName: s_projectNameSingleFileSample);

            var solution = new AdhocWorkspace()
                .CurrentSolution.AddProject(
                    projectId,
                    s_projectNameSingleFileSample,
                    s_projectNameSingleFileSample,
                    LanguageNames.CSharp
                )
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference)
                .AddMetadataReference(projectId, s_cSharpSymbolsReference)
                .AddMetadataReference(projectId, s_codeAnalysisReference);

            foreach (var file in files)
            {
#pragma warning disable CS0618
                var documentId = DocumentId.CreateNewId(projectId, debugName: file.FilePath);
                solution = solution.AddDocument(documentId, file.FilePath, file.Text);
#pragma warning restore CS0618
            }

            return solution.GetProject(projectId)!;
        }

        /// <summary>
        /// Creates a project from raw source code plus optional dictionary/config files declared as
        /// Roslyn <c>AdditionalFiles</c> (with real file paths so directory resolution works).
        /// </summary>
        protected static Project CreateProjectWithCode(
            string code,
            string codeFilePath,
            params (string Path, string Content)[] additionalFiles
        )
        {
            var projectId = ProjectId.CreateNewId(debugName: s_projectNameSingleFileSample);

            var solution = new AdhocWorkspace()
                .CurrentSolution.AddProject(
                    projectId,
                    s_projectNameSingleFileSample,
                    s_projectNameSingleFileSample,
                    LanguageNames.CSharp
                )
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference)
                .AddMetadataReference(projectId, s_cSharpSymbolsReference)
                .AddMetadataReference(projectId, s_codeAnalysisReference);

            var documentId = DocumentId.CreateNewId(projectId, debugName: codeFilePath);
            solution = solution.AddDocument(
                documentId,
                Path.GetFileName(codeFilePath),
                SourceText.From(code),
                filePath: codeFilePath
            );

            foreach (var (path, content) in additionalFiles)
            {
                var additionalDocumentId = DocumentId.CreateNewId(projectId, debugName: path);
                solution = solution.AddAdditionalDocument(
                    additionalDocumentId,
                    Path.GetFileName(path),
                    SourceText.From(content),
                    filePath: path
                );
            }

            return solution.GetProject(projectId)!;
        }

        protected static Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            Project project,
            params DiagnosticAnalyzer[] analyzers
        ) => GetDiagnosticsAsync(project, ImmutableArray.CreateRange(analyzers));

        private static async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(
            Project project,
            ImmutableArray<DiagnosticAnalyzer> analyzers
        )
        {
            var compilation =
                await project.GetCompilationAsync()
                ?? throw new InvalidOperationException("Cann't get project complication");
            // Pass the project's AnalyzerOptions so AdditionalFiles reach the analyzer.
            return await compilation.WithAnalyzers(analyzers, project.AnalyzerOptions).GetAnalyzerDiagnosticsAsync();
        }
    }
}
