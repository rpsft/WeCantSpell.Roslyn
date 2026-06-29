using System.Collections.Generic;
using System.Threading.Tasks;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp.Parsing
{
    public class BilingualCommentsSpellingTests : CSharpParsingTestBase
    {
        public static object[][] CanFindErrorsInBilingualCommentsData => new[] { new object[] { "Рtеализация", 4, 9 } };

        [Theory, MemberData(nameof(CanFindErrorsInBilingualCommentsData))]
        public async Task CanFindErrorsInBilingualComments(string expectedWord, int expectedLine, int expectedCharacter)
        {
            var analyzer = new SpellingAnalyzerCSharp(
                new ConfigurableSpellChecker(
                    new SpellCheckerOptions
                    {
                        LanguageCodes = new HashSet<string> { "en-US", "ru-RU" },
                    }
                )
            );

            var project = await ReadCodeFileAsProjectAsync("BilingualComments.Example.csx");

            project.Should().NotBeNull();

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);
            using (new AssertionScope())
            {
                diagnostics
                    .Should()
                    .ContainSingle()
                    .Subject.Should()
                    .HaveId("SP3113")
                    .And.HaveLineLocation(
                        expectedLine,
                        expectedCharacter,
                        expectedWord.Length,
                        "BilingualComments.Example.csx"
                    )
                    .And.HaveMessageContaining(expectedWord);
            }
        }
    }
}
