using WeCantSpell.Hunspell;

namespace WeCantSpell.Roslyn
{
    /// <summary>
    /// Word list provider that wraps an already-parsed <see cref="WordList"/>.
    /// </summary>
    /// <remarks>
    /// Used for dictionaries read from Roslyn <c>AdditionalFiles</c>. Unlike
    /// <see cref="FileWordListProvider"/> it holds no file path and performs no disk polling:
    /// the content is supplied by Roslyn and its change tracking drives cache invalidation.
    /// </remarks>
    internal sealed class StaticWordListProvider : IWordListProvider
    {
        public WordList WordList { get; }

        public StaticWordListProvider(WordList wordList)
        {
            WordList = wordList;
        }
    }
}
