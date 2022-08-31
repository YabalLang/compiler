using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record VariableDeclarationStatement(SourceRange Range, string Name, Expression? Value = null, LanguageType? Type = null, IConstantValue? ConstantValue = null) : Statement(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Value?.BeforeBuild(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        var block = builder.Block;
        var pointer = block.IsGlobal
            ? builder.GetGlobalVariable(block.StackOffset++)
            : builder.GetStackVariable(block.StackOffset++);

        LanguageType? valueType = null;

        if (Value != null)
        {
            valueType = Value.BuildExpression(builder, false);
            builder.StoreA(pointer);
            builder.SetComment($"store value in variable '{Name}'");
        }

        if (valueType == null)
        {
            throw new InvalidOperationException("No type specified");
        }

        if (Type != null && valueType != Type)
        {
            if (valueType == LanguageType.Assembly)
            {
                valueType = Type;
            }
            else
            {
                throw new InvalidOperationException("Type mismatch");
            }
        }

        var variable = new Variable(Name, pointer, valueType, ConstantValue);
        builder.Block.DeclareVariable(Name, variable);
        pointer.AssignedVariables.Add(variable);
    }
}
