using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record Expression(SourceRange Range) : Node(Range)
{
    public sealed override void Build(YabalBuilder builder)
    {
        BuildExpression(builder, false);
    }

    public abstract LanguageType BuildExpression(YabalBuilder builder, bool isVoid);

    public virtual Expression Optimize() => this;
}
