using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record IdentifierExpression(SourceRange Range, string Name) : Expression(Range), IExpressionToB
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (!builder.TryGetVariable(Name, out var variable))
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.UndefinedVariable(Name));
            builder.SetA(0);
            return LanguageType.Integer;
        }

        builder.LoadA(variable.Pointer);

        builder.SetComment(
            variable.Type.StaticType == StaticType.Pointer
                ? $"load pointer address from variable '{Name}'"
                : $"load variable '{Name}'"
        );

        return variable.Type;
    }

    public LanguageType BuildExpressionToB(YabalBuilder builder)
    {
        if (!builder.TryGetVariable(Name, out var variable))
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.UndefinedVariable(Name));
            builder.SetB(0);
            return LanguageType.Integer;
        }

        builder.LoadB(variable.Pointer);
        return variable.Type;
    }

    public override Expression Optimize(BlockCompileStack block)
    {
        if (block.TryGetConstant(Name, out var constantExpression))
        {
            return constantExpression;
        }

        return base.Optimize(block);
    }

    public override bool OverwritesB => false;

    bool IExpressionToB.OverwritesA => false;
}
