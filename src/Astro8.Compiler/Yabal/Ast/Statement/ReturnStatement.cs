using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Build(YabalBuilder builder)
    {
        if (builder.Block.Function is not {} function)
        {
            builder.AddError(ErrorLevel.Error, Expression.Range, ErrorMessages.ReturnOutsideFunction);
            return;
        }

        var type = Expression.BuildExpression(builder, false);

        if (function.ReturnType != type)
        {
            builder.AddError(ErrorLevel.Error, Expression.Range, ErrorMessages.InvalidType(function.ReturnType, type));
        }
    }
}
