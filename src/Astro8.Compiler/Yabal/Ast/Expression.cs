using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record Expression(SourceRange Range) : Node(Range)
{
    public sealed override void Build(YabalBuilder builder)
    {
        BuildExpression(builder);
    }

    public abstract LanguageType BuildExpression(YabalBuilder builder);

    public virtual Expression Optimize() => this;
}