﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp
{
    public abstract class CSharpTestBase
    {
        private static readonly string s_pathBase = $"{typeof(CSharpTestBase).Namespace}";
        private static readonly string s_projectNameSingleFileSample = nameof(s_projectNameSingleFileSample);

        private static readonly MetadataReference s_corlibReference = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference s_systemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference s_cSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).GetTypeInfo().Assembly.Location);
        private static readonly MetadataReference s_codeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).GetTypeInfo().Assembly.Location);

        protected Stream OpenCodeFileStream(string embeddedResourceFileName) =>
            typeof(CSharpTestBase).GetTypeInfo().Assembly.GetManifestResourceStream(s_pathBase + "." + embeddedResourceFileName);

        protected async Task<string> ReadCodeFileAsStringAsync(string embeddedResourceFileName)
        {
            await using var stream = OpenCodeFileStream(embeddedResourceFileName);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return await reader.ReadToEndAsync();
        }

        protected abstract string CreateResourceNameFromFileName(string fileName);

        protected async Task<TextAndVersion> ReadCodeFileAsSTextAndVersionAsync(string fileName) =>
            TextAndVersion.Create(
                SourceText.From(await ReadCodeFileAsStringAsync(CreateResourceNameFromFileName(fileName))),
                VersionStamp.Default,
                fileName);

        protected async Task<Project> ReadCodeFileAsProjectAsync(string embeddedResourceFileName) =>
            CreateProjectWithFiles(new[] { await ReadCodeFileAsSTextAndVersionAsync(embeddedResourceFileName) });

        protected Project CreateProjectWithFiles(IEnumerable<TextAndVersion> files)
        {
            var projectId = ProjectId.CreateNewId(debugName: s_projectNameSingleFileSample);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, s_projectNameSingleFileSample, s_projectNameSingleFileSample, LanguageNames.CSharp)
                .AddMetadataReference(projectId, s_corlibReference)
                .AddMetadataReference(projectId, s_systemCoreReference)
                .AddMetadataReference(projectId, s_cSharpSymbolsReference)
                .AddMetadataReference(projectId, s_codeAnalysisReference);

            foreach (var file in files)
            {
                var documentId = DocumentId.CreateNewId(projectId, debugName: file.FilePath);
                solution = solution.AddDocument(documentId, file.FilePath, file.Text);
            }

            return solution.GetProject(projectId);
        }

        protected Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Project project, params DiagnosticAnalyzer[] analyzers) =>
            GetDiagnosticsAsync(project, ImmutableArray.CreateRange(analyzers));

        protected async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Project project, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            var compilation = await project.GetCompilationAsync();
            return await compilation
                .WithAnalyzers(analyzers)
                .GetAnalyzerDiagnosticsAsync();
        }
    }
}
