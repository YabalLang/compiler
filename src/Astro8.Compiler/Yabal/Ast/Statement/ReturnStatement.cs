using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);

        if (builder.Block.Return == null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ReturnOutsideFunction);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.BuildExpression(builder, false);

        if (builder.Block.Return != null)
        {
            builder.Jump(builder.Block.Return);
        }
    }

    public override Statement CloneStatement()
    {
        return new ReturnStatement(Range, Expression.CloneExpression());
    }

    public override Statement Optimize()
    {
        return new ReturnStatement(Range, Expression.Optimize());
    }
}
