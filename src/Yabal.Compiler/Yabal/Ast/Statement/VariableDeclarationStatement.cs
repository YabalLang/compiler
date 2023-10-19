using Yabal.Exceptions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record VariableDeclarationStatement(SourceRange Range, Identifier Name, bool Constant, Expression? Value = null, LanguageType? Type = null) : Statement(Range)
{
    public Variable Variable { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        Value?.Initialize(builder);

        var type = Type ?? Value?.Type;

        if (type is null)
        {
            throw new InvalidCodeException("Variable type is not specified", Name.Range);
        }

        Variable = builder.CreateVariable(Name, type, Value);
    }

    public override void Build(YabalBuilder builder)
    {
        if (Value == null || Variable.CanBeRemoved)
        {
            return;
        }

        if (Value is IPointerSource createPointer)
        {
            createPointer.BuildExpression(builder, false, Type);
            builder.StoreA(Variable.Pointer);

            builder.SetA(createPointer.Bank);
            builder.StoreA(Variable.Pointer.Add(1));
        }
        else
        {
            builder.SetValue(Variable.Pointer, Variable.Type, Value);
        }
    }

    public override Statement CloneStatement()
    {
        return new VariableDeclarationStatement(Range, Name, Constant, Value?.CloneExpression(), Type);
    }

    public override Statement Optimize()
    {
        return new VariableDeclarationStatement(Range, Name, Constant, Value?.Optimize(), Type)
        {
            Variable = Variable
        };
    }
}
