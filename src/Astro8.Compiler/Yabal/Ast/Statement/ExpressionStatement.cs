using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ExpressionStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Expression.BeforeBuild(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.Build(builder);
    }
}
