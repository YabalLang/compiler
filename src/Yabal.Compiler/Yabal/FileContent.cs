using Yabal.Ast;

namespace Yabal;

public interface IFileLoader
{
    ValueTask<FileContent> LoadAsync(SourceRange range, string path, FileReader reader);
}

public record FileContent(int Offset, int[] Data);
