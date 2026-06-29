using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Tests.Utilities;

namespace WeCantSpell.Roslyn.Tests.SpellChecker
{
    [TestCategory("SpellChecker")]
    public class AdditionalFilesDictionaryIndexTests
    {
        private sealed class TestAdditionalText : AdditionalText
        {
            private readonly string _content;

            public TestAdditionalText(string path, string content = "")
            {
                Path = path;
                _content = content;
            }

            public override string Path { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) =>
                SourceText.From(_content);
        }

        private static ImmutableArray<AdditionalText> Files(params AdditionalText[] files) =>
            ImmutableArray.CreateRange<AdditionalText>(files);

        [Fact]
        public void Resolve_returns_unmatched_when_no_files_declared()
        {
            var index = AdditionalFilesDictionaryIndex.Build(ImmutableArray<AdditionalText>.Empty);

            var resolution = index.Resolve("/WCSTTests/Project/src/MyClass.cs");

            resolution.Matched.Should().BeFalse();
            resolution.Dictionaries.Should().BeEmpty();
        }

        [Fact]
        public void Resolve_matches_dictionary_in_same_directory_as_source_file()
        {
            var dic = new TestAdditionalText("/WCSTTests/Project/src/.directory.dic", "Bazinga");
            var index = AdditionalFilesDictionaryIndex.Build(Files(dic));

            var resolution = index.Resolve("/WCSTTests/Project/src/MyClass.cs");

            resolution.Matched.Should().BeTrue();
            resolution.Dictionaries.Should().HaveCount(1);
        }

        [Fact]
        public void Resolve_returns_nearest_directory_first()
        {
            var nearDic = new TestAdditionalText("/WCSTTests/Project/src/.directory.dic");
            var farDic = new TestAdditionalText("/WCSTTests/Project/.directory.dic");
            var index = AdditionalFilesDictionaryIndex.Build(Files(nearDic, farDic));

            var resolution = index.Resolve("/WCSTTests/Project/src/MyClass.cs");

            resolution.Matched.Should().BeTrue();
            resolution.Dictionaries.Should().HaveCount(2);
            resolution.Dictionaries[0].Path.Should().Be("/WCSTTests/Project/src/.directory.dic");
            resolution.Dictionaries[1].Path.Should().Be("/WCSTTests/Project/.directory.dic");
        }

        [Fact]
        public void Resolve_isRoot_config_stops_ancestor_walk()
        {
            var rootConfig = new TestAdditionalText("/WCSTTests/Project/.wecantspell", @"{""isRoot"":true}");
            var ancestorDic = new TestAdditionalText("/WCSTTests/.directory.dic");
            var index = AdditionalFilesDictionaryIndex.Build(Files(rootConfig, ancestorDic));

            var resolution = index.Resolve("/WCSTTests/Project/src/MyClass.cs");

            resolution.Matched
                .Should()
                .BeFalse("isRoot at /WCSTTests/Project stops the walk before reaching /WCSTTests/");
        }

        [Theory]
        [InlineData(".spelling.dic")]
        [InlineData(".directory.dic")]
        [InlineData(".wecantspell.dic")]
        public void Resolve_recognizes_all_dictionary_file_names(string fileName)
        {
            var dic = new TestAdditionalText($"/WCSTTests/Project/{fileName}");
            var index = AdditionalFilesDictionaryIndex.Build(Files(dic));

            var resolution = index.Resolve("/WCSTTests/Project/MyClass.cs");

            resolution.Matched.Should().BeTrue($"{fileName} should be recognized as a dictionary");
        }

        [Fact]
        public void Resolve_relative_path_dictionary_is_global_and_matches_any_tree()
        {
            var globalDic = new TestAdditionalText(".directory.dic", "word");
            var index = AdditionalFilesDictionaryIndex.Build(Files(globalDic));

            var resolution = index.Resolve("/WCSTTests/AnyProject/src/MyClass.cs");

            resolution.Matched.Should().BeTrue("non-rooted paths are treated as global dictionaries");
        }

        [Fact]
        public void Resolve_dictionary_in_unrelated_directory_does_not_match()
        {
            var dic = new TestAdditionalText("/WCSTTests/ProjectA/src/.directory.dic");
            var index = AdditionalFilesDictionaryIndex.Build(Files(dic));

            var resolution = index.Resolve("/WCSTTests/ProjectB/src/MyClass.cs");

            resolution.Matched.Should().BeFalse("ProjectB should not see ProjectA's dictionary");
        }
    }
}
