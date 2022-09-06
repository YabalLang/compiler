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

        var valueType = Type;

        if (Value != null)
        {
            valueType = Value.BuildExpression(builder, false);
        }

        if (valueType == null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.VariableTypeNotSpecified);
            valueType = LanguageType.Integer;
        }

        var pointer = block.IsGlobal
            ? builder.Globals.GetNext(block, valueType)
            : builder.Stack.GetNext(block, valueType);

        if (Value != null)
        {
            builder.StoreA(pointer);
            builder.SetComment($"store value in variable '{Name}'");
        }

        if (Type != null && valueType != Type)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.InvalidType(Type, valueType));
        }

        var variable = new Variable(Name, pointer, valueType);
        builder.Block.DeclareVariable(Name, variable);
        pointer.AssignedVariables.Add(variable);
    }
}
