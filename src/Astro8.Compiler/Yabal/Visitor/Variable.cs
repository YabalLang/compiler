using Astro8.Instructions;
using Astro8.Yabal.Ast;

namespace Astro8.Yabal.Visitor;

public record Variable(string Name, InstructionPointer Pointer, LanguageType Type, IConstantValue? ConstantValue = null)
{
    public bool Constant { get; set; } = true;
}
