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
        var block = builder.Block;
        InstructionPointer pointer;

        if (block.IsGlobal)
        {
            builder.EmitRaw(0);
            pointer = builder.CreatePointer(Name);
        }
        else
        {
            pointer = builder.GetStackVariable(block.StackOffset++);
        }

        LanguageType? valueType = null;

        if (Value != null)
        {
            valueType = Value.BuildExpression(builder, false);
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
