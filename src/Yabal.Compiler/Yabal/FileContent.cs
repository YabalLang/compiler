using Yabal.Ast;

namespace Yabal;

public interface IFileLoader
{
    ValueTask<FileContent> LoadAsync(YabalBuilder builder, SourceRange range, string path, FileReader reader);
}

public record FileContent(int Offset, int[] Data);
