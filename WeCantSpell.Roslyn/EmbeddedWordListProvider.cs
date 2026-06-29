using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using WeCantSpell.Hunspell;

namespace WeCantSpell.Roslyn
{
    /// <summary>
    /// Word list provider for dictionaries embedded in resources
    /// </summary>
    public class EmbeddedWordListProvider : IWordListProvider
    {
        public WordList WordList { get; }

        // Embedded dictionaries never change, so they can be decompressed once and reused for the
        // lifetime of the process. This keeps building per-tree spell checkers cheap.
        private static readonly ConcurrentDictionary<string, EmbeddedWordListProvider> s_cache = new();

        private EmbeddedWordListProvider(WordList wordList)
        {
            WordList = wordList;
        }

        public static EmbeddedWordListProvider Load(string languageCode)
        {
            return s_cache.GetOrAdd(languageCode, LoadCore);
        }

        private static EmbeddedWordListProvider LoadCore(string languageCode)
        {
            const string resourceNamespaceBase = "";
            string languageResourceName = resourceNamespaceBase + languageCode;
            string affName = languageResourceName + ".aff.gz";
            string dicName = languageResourceName + ".dic.gz";

            Assembly assembly = typeof(EmbeddedSpellChecker).GetTypeInfo().Assembly;

            using Stream affCompressedStream =
                assembly.GetManifestResourceStream(affName) ?? throw new InvalidOperationException();
            using var affStream = new GZipStream(affCompressedStream, CompressionMode.Decompress);
            using Stream dicCompressedStream =
                assembly.GetManifestResourceStream(dicName) ?? throw new InvalidOperationException();
            using var dicStream = new GZipStream(dicCompressedStream, CompressionMode.Decompress);
            var wordList = WordList.CreateFromStreams(dicStream, affStream);
            return new EmbeddedWordListProvider(wordList);
        }
    }
}
