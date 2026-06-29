using System.Threading.Tasks;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.Integration.CSharp.Parsing
{
    public class AnonymousObjectsSpellingTests : CSharpParsingTestBase
    {
        public static object[][] CanFindMistakesInAnonymousMembersData =>
            new[]
            {
                new object[] { "Count", 9, 17 },
                new object[] { "Distance", 10, 17 },
                new object[] { "Nested", 11, 17 },
                new object[] { "Value", 13, 21 },
            };

        [Theory, MemberData(nameof(CanFindMistakesInAnonymousMembersData))]
        public async Task can_find_mistakes_in_anonymous_members(
            string expectedWord,
            int expectedLine,
            int expectedCharacter
        )
        {
            var analyzer = new SpellingAnalyzerCSharp(new WrongWordChecker(expectedWord));
            var project = await ReadCodeFileAsProjectAsync("AnonymousObjects.SimpleExamples.csx");

            var diagnostics = await GetDiagnosticsAsync(project, analyzer);

            using (new AssertionScope())
            {
                diagnostics
                    .Should()
                    .ContainSingle()
                    .Subject.Should()
                    .HaveId("SP3110")
                    .And.HaveLineLocation(
                        expectedLine,
                        expectedCharacter,
                        expectedWord.Length,
                        "AnonymousObjects.SimpleExamples.csx"
                    )
                    .And.HaveMessageContaining(expectedWord);
            }
        }
    }
}
