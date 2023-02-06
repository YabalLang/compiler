using Yabal.Ast;
using Yabal.Instructions;

namespace Yabal.Visitor;

public record Variable(
    Identifier Identifier,
    InstructionPointer Pointer,
    LanguageType Type,
    Expression? Initializer = null,
    bool IsGlobal = false)
{
    public bool Constant { get; set; } = true;

    public int Usages { get; set; }

    public bool HasBeenUsed { get; set; }

    public bool CanBeRemoved => HasBeenUsed && Usages == 0;

    public List<IdentifierExpression> References { get; } = new();
}
