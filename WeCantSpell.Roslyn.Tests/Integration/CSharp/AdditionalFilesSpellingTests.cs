using System.Threading.Tasks;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp
{
    public class AdditionalFilesSpellingTests : CSharpTestBase
    {
        protected override string CreateResourceNameFromFileName(string fileName) =>
            throw new NotSupportedException("This test class does not use embedded resources.");

        private const string CodeFilePath = "/WCSTTests/Project/src/MyClass.cs";
        private const string DictFilePath = "/WCSTTests/Project/src/.directory.dic";

        private const string CodeWithMisspelledIdentifier =
            @"
namespace MyProject
{
    class Bazinga { }
}
";

        [Fact]
        public async Task word_in_additional_files_dictionary_suppresses_sp3110()
        {
            var analyzer = new SpellingAnalyzerCSharp();
            var project = CreateProjectWithCode(CodeWithMisspelledIdentifier, CodeFilePath, (DictFilePath, "Bazinga"));

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);

            diagnostics
                .Should()
                .NotContain(d => d.Id == "SP3110", "\"Bazinga\" is listed in the AdditionalFiles dictionary");
        }

        [Fact]
        public async Task word_absent_from_empty_dictionary_fires_sp3110()
        {
            var analyzer = new SpellingAnalyzerCSharp();
            var project = CreateProjectWithCode(CodeWithMisspelledIdentifier, CodeFilePath, (DictFilePath, ""));

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);

            diagnostics.Should().Contain(d => d.Id == "SP3110", "\"Bazinga\" is not a recognized English word");
        }

        [Fact]
        public async Task same_analyzer_instance_picks_up_dictionary_change_between_compilations()
        {
            // Regression guard for RSSL-11794: VBCSCompiler reuses the same analyzer instance
            // across builds. With the AdditionalFiles path the word list is keyed on SourceText
            // identity via ConditionalWeakTable, so editing the dictionary produces a new SourceText
            // object → fresh parse, without needing a build-server restart.
            var analyzer = new SpellingAnalyzerCSharp();

            // Pass 1 – empty dictionary, "Bazinga" is not recognized → SP3110
            var projectEmpty = CreateProjectWithCode(CodeWithMisspelledIdentifier, CodeFilePath, (DictFilePath, ""));
            var diagnosticsEmpty = await GetDiagnosticsAsync(projectEmpty, analyzer);

            // Pass 2 – same analyzer instance, dictionary now contains "Bazinga" → no SP3110
            var projectWithWord = CreateProjectWithCode(
                CodeWithMisspelledIdentifier,
                CodeFilePath,
                (DictFilePath, "Bazinga")
            );
            var diagnosticsWithWord = await GetDiagnosticsAsync(projectWithWord, analyzer);

            diagnosticsEmpty
                .Should()
                .Contain(d => d.Id == "SP3110", "pass 1: dictionary was empty, \"Bazinga\" was not recognized");
            diagnosticsWithWord
                .Should()
                .NotContain(
                    d => d.Id == "SP3110",
                    "pass 2: same analyzer instance must pick up the updated dictionary content"
                );
        }
    }
}
