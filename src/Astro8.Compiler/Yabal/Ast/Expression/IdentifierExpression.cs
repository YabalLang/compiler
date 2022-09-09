using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record IdentifierExpression(SourceRange Range, string Name) : AddressExpression(Range), IExpressionToB, IConstantValue
{
    private Variable? _variable;

    public Variable Variable => _variable ?? throw new InvalidOperationException("Variable not set");

    public override void Initialize(YabalBuilder builder)
    {
        _variable = builder.GetVariable(Name);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        builder.LoadA(Variable.Pointer);
    }

    public override void MarkModified()
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

    public override int? Bank => Pointer?.Bank;

    public override void StoreAddressInA(YabalBuilder builder)
    {
        Variable.Usages++;
        builder.SetA(Variable.Pointer);
    }

    public override string ToString()
    {
        return Name;
    }

    public object? Value => _variable is { Constant: true, Initializer: {} initializer }
        ? initializer.Optimize() is IConstantValue { Value: var value }
            ? value
            : null
        : null;

    public override Pointer? Pointer
    {
        get
        {
            Variable.Usages++;

            if (Variable.Type.StaticType == StaticType.Pointer)
            {
                return (Value as IAddress)?.Pointer;
            }

            return Variable.Pointer;
        }
    }

    public override Expression Optimize()
    {
        if (_variable == null)
        {
            return this;
        }

        _variable.HasBeenUsed = true;

        switch (Value)
        {
            case int intValue:
                return new IntegerExpression(Range, intValue);
            case bool boolValue:
                return new BooleanExpression(Range, boolValue);
            default:
                _variable.Usages++;
                return this;
        }
    }

    public override IdentifierExpression CloneExpression()
    {
        return new IdentifierExpression(Range, Name)
        {
            _variable = _variable
        };
    }
}
