using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public interface IAddress
{
    Either<int, InstructionPointer>? Get(YabalBuilder builder);

    int? Length { get; }

    int? GetValue(int offset);
}
