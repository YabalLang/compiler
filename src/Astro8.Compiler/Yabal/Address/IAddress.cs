using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IAddress
{
    Pointer? Pointer { get; }

    int? Length { get; }

    LanguageType Type { get; }

    int? GetValue(int offset);
}
