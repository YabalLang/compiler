using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.BuildExpression(builder, false);
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
