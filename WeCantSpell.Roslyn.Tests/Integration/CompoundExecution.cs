using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration
{
    [TestCategory("Compound")]
    public class CompoundExecution
    {
        private static string? SearchForFile(string fileName)
        {
            var directory = new DirectoryInfo(".");
            do
            {
                var filePath = Path.Combine(directory.FullName, fileName);
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                directory = directory.Parent;
            } while (directory != null);

            return null;
        }

        private readonly Solution _solution;

        public CompoundExecution()
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();
            var workspace = MSBuildWorkspace.Create();
            const string fileName = "WeCantSpell.Roslyn.sln";
            var solutionFilePath = SearchForFile(fileName);
            if (solutionFilePath == null)
                throw new InvalidOperationException($"Can't find {fileName} in current directory or its parents");
            _solution = workspace.OpenSolutionAsync(solutionFilePath).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task ShouldCheckSolution()
        {
            var analyzer = new SpellingAnalyzerCSharp(LengthWordChecker.Four);
            await Task.WhenAll(_solution.Projects.Select(p => FindSpellingMistakesForProject(p, analyzer)));
        }

        [Fact]
        public async Task ShouldCheckSingleProject()
        {
            var analyzer = new SpellingAnalyzerCSharp();
            var project = _solution.Projects.First(p => p.Name.EndsWith("Roslyn"));
            var mistakesForProject = await FindSpellingMistakesForProject(project, analyzer);
            mistakesForProject.Should().NotBeEmpty();
        }

        private static async Task<ImmutableArray<Diagnostic>> FindSpellingMistakesForProject(
            Project project,
            DiagnosticAnalyzer analyzer
        )
        {
            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            var diagnostics = await (compilation ?? throw new InvalidOperationException())
                .WithAnalyzers([analyzer])
                .GetAnalyzerDiagnosticsAsync()
                .ConfigureAwait(false);

            return diagnostics;
        }

        public class LengthWordChecker : ISpellChecker
        {
            public static LengthWordChecker Four { get; } = new(4);

            private LengthWordChecker(int length) => Length = length;

            private int Length { get; }

            public bool Check(string? word) => word != null && word.Length % Length != 0;

            public IEnumerable<string> Suggest(string word) => Enumerable.Empty<string>();
        }
    }
}
