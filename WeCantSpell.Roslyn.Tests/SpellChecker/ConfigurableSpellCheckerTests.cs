using System.Collections.Generic;
using System.Threading.Tasks;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.SpellChecker
{
    [TestCategory("SpellChecker")]
    public class ConfigurableSpellCheckerTests
    {
        [Fact]
        public void ShouldReadLanguageListFromFileSystem()
        {
            var options = new SpellCheckerOptions(
                Path.Combine(Directory.GetCurrentDirectory(), "SpellChecker", "TestDirectory")
            );
            using (new AssertionScope())
            {
                options.LanguageCodes.Should().BeEquivalentTo(new[] { "en-US", "ru-RU" });
                options.AdditionalDictionaryPaths.Should().HaveCount(1);
            }
        }

        [Fact]
        public void ShouldReadDictionaryFromFileSystem()
        {
            var spellchecker = new ConfigurableSpellChecker(
                new SpellCheckerOptions
                {
                    LanguageCodes = new HashSet<string>(),
                    AdditionalDictionaryPaths = new List<string>
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), "SpellChecker", "Files", "FantasyWords.dic"),
                    },
                }
            );
            spellchecker.Check("Bazinga").Should().BeTrue();
            spellchecker.Check("Froomplestoot").Should().BeFalse();
        }

        [Fact]
        public void ShouldReadDictionaryFromResourceFileSystem()
        {
            var fileSystem = new ResourceFileSystem();
            var spellchecker = new ConfigurableSpellChecker(
                new SpellCheckerOptions(fileSystem, "SpellChecker.Files")
                {
                    LanguageCodes = new HashSet<string>(),
                    AdditionalDictionaryPaths = new List<string> { "Files.FantasyWords.dic" },
                }
            );
            spellchecker.Check("Bazinga").Should().BeTrue();
            spellchecker.Check("Froomplestoot").Should().BeFalse();
        }

        [Fact]
        public async Task ShouldWatchForChangesInDictionaries()
        {
            // Arrange
            var fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dic");
            await File.WriteAllTextAsync(fileName, "");
            var spellchecker = new ConfigurableSpellChecker(
                new SpellCheckerOptions
                {
                    LanguageCodes = new HashSet<string>(),
                    AdditionalDictionaryPaths = new List<string> { fileName },
                }
            );
            // Act
            var resultBefore = spellchecker.Check("Bazinga");
            await spellchecker.AddToLocalDictionaryAsync("Bazinga");
            var resultAfterApiUpdate = spellchecker.Check("Bazinga");
            await File.WriteAllTextAsync(fileName, "Froomplestoot");
            // FileWordListProvider throttles re-reads (~100 ms), so wait comfortably past that
            // window before checking. We deliberately do not assert that the change is still
            // invisible *before* the window elapses: that depends on the preceding operations
            // finishing within 100 ms of wall-clock time, which is not guaranteed on slow or
            // loaded CI runners and made this test flaky.
            await Task.Delay(300);
            var resultAfterDirectUpdateAndWait = spellchecker.Check("Froomplestoot");
            // Assert
            resultBefore.Should().BeFalse("the dictionary not yet contained a new word");
            resultAfterApiUpdate.Should().BeTrue("the dictionary has been updated through API");
            resultAfterDirectUpdateAndWait
                .Should()
                .BeTrue("the spell checker picks up direct file writes after the refresh window elapses");
        }
    }
}
