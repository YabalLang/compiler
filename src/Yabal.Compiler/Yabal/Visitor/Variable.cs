using Yabal.Ast;
using Yabal.Instructions;

namespace Yabal.Visitor;

public interface IVariable
{
    bool IsGlobal { get; }

    bool IsDirectReference { get; }

    Identifier Identifier { get; }

    Pointer Pointer { get; }

    LanguageType Type { get; }

    Expression? Initializer { get; }

    bool ReadOnly { get; }

    public bool Constant { get; set; }

    IEnumerable<Identifier> References { get; }

    void AddReference(Identifier identifierExpression);

    void AddUsage();

    void MarkUsed();
}

public record Variable(
    Identifier Identifier,
    InstructionPointer Pointer,
    LanguageType Type,
    Expression? Initializer = null,
    bool IsGlobal = false,
    bool IsDirectReference = false)
    : IVariable
{
    Pointer IVariable.Pointer => Pointer;

    public bool ReadOnly => false;

    public bool Constant { get; set; } = true;

    public int Usages { get; set; }

    public bool HasBeenUsed { get; set; }

    public bool CanBeRemoved => HasBeenUsed && Usages == 0;

    public List<Identifier> References { get; } = new();

    IEnumerable<Identifier> IVariable.References => References;

    void IVariable.AddReference(Identifier identifierExpression)
    {
        References.Add(identifierExpression);
    }

    void IVariable.AddUsage()
    {
        Usages++;
    }

    void IVariable.MarkUsed()
    {
        HasBeenUsed = true;
    }
}
