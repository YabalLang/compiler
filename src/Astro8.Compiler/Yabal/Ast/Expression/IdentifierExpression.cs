using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record IdentifierExpression(SourceRange Range, string Name) : Expression(Range), IExpressionToB, IAddressExpression, IConstantValue
{
    public Variable Variable { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        Variable = builder.GetVariable(Name);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.LoadA(Variable.Pointer);
    }

    public void MarkModified()
    {
        Variable.Constant = false;
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
    {
        builder.LoadB(Variable.Pointer);
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => Variable.Type;

    bool IExpressionToB.OverwritesA => false;

    public void StoreAddressInA(YabalBuilder builder)
    {
        builder.SetA(Variable.Pointer);
    }

    public override string ToString()
    {
        return Name;
    }

    public object? Value => Variable is { Constant: true, ConstantValue: {} constantValue }
        ? constantValue.Value
        : null;


    public Pointer? Pointer
    {
        get
        {
            if (Variable.Type.StaticType == StaticType.Array)
            {
                return Variable.Constant
                    ? (Variable.ConstantValue?.Value as IAddress)?.Pointer
                    : null;
            }

            return Variable.Pointer;
        }
    }
}
