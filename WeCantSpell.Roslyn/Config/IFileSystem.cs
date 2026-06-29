using System.Collections.Generic;
using System.IO;

namespace WeCantSpell.Roslyn.Config
{
    public interface IFileSystem
    {
        string FileSystemType { get; }
        string? GetDirectoryName(string path);
        string? GetParentDirectory(string directory);
        string? ReadAllText(string filePath);
        string CombinePath(string directoryName, string fileName);
        bool FileExists(string tryFilePath);
        Stream ReadStream(string path);
        string ChangeExtension(string path, string extension);
        IEnumerable<string> GetFiles(string directory, string pattern, SearchOption searchOption);
        bool DirectoryExists(string path);
        StreamWriter AppendText(string path);
    }
}
