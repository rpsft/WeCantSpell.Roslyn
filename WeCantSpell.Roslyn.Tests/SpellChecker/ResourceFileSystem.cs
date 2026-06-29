using System.Collections.Generic;
using System.Reflection;
using System.Text;
using WeCantSpell.Roslyn.Config;
using WeCantSpell.Roslyn.Tests.Integration.CSharp;

namespace WeCantSpell.Roslyn.Tests.SpellChecker
{
    public class ResourceFileSystem : IFileSystem
    {
        public string FileSystemType => "Resource";

        private static readonly string s_pathBase = $"{typeof(ResourceFileSystem).Namespace}";

        public string GetDirectoryName(string path)
        {
            return "";
        }

        public string? GetParentDirectory(string directory)
        {
            return null;
        }

        private static Stream? OpenCodeFileStream(string embeddedResourceFileName) =>
            typeof(CSharpTestBase)
                .GetTypeInfo()
                .Assembly.GetManifestResourceStream(s_pathBase + "." + embeddedResourceFileName);

        public string? ReadAllText(string filePath)
        {
            using var stream = OpenCodeFileStream(filePath);
            if (stream == null)
                return null;
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }

        public string CombinePath(string directoryName, string fileName)
        {
            return $"{directoryName}.{fileName}";
        }

        public bool FileExists(string tryFilePath)
        {
            using var stream = OpenCodeFileStream(tryFilePath);
            return stream != null;
        }

        public Stream ReadStream(string path)
        {
            return OpenCodeFileStream(path)
                ?? throw new InvalidOperationException($"Resource {s_pathBase}.{path} not found");
        }

        public string ChangeExtension(string path, string extension)
        {
            int index = path.LastIndexOf('.');

            return index != -1 ? path[..index] + extension : path;
        }

        public IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption)
        {
            return Array.Empty<string>();
        }

        public bool DirectoryExists(string path)
        {
            return false;
        }

        public StreamWriter AppendText(string path)
        {
            throw new NotImplementedException();
        }
    }
}
