using Yabal.Ast;

namespace Yabal;

public interface IAddress
{
    Pointer? Pointer { get; }

    int? Length { get; }

    LanguageType Type { get; }

    int? GetValue(int offset);
}
