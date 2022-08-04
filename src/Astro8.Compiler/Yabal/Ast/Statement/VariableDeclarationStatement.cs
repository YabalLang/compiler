using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record VariableDeclarationStatement(SourceRange Range, string Name, Expression? Value = null, LanguageType? Type = null) : Statement(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value?.BeforeBuild(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        LanguageType? valueType = null;
        Expression? expression = null;
        var value = 0;

        switch (Value)
        {
            case IntegerExpression { Value: var intValue }:
                value = intValue;
                valueType = LanguageType.Integer;
                break;
            case BooleanExpression { Value: var boolExpression }:
                value = boolExpression ? 1 : 0;
                valueType = LanguageType.Boolean;
                break;
            default:
                expression = Value;
                break;
        }

        builder.EmitRaw(value);
        var pointer = builder.CreatePointer(Name);

        if (expression != null)
        {
            valueType = expression.BuildExpression(builder);
            builder.StoreA(pointer);
        }

        if (valueType == null)
        {
            throw new InvalidOperationException("No type specified");
        }

        if (Type != null && valueType != Type)
        {
            throw new InvalidOperationException("Type mismatch");
        }

        builder.Block.DeclareVariable(Name, new Variable(pointer, valueType));
    }
}
