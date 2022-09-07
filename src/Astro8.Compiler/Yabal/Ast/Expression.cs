using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public abstract record Expression(SourceRange Range) : Node(Range), IExpression
{
    public sealed override void Build(YabalBuilder builder)
    {
        BuildExpression(builder, false);
    }

    public void BuildExpression(YabalBuilder builder, bool isVoid)
    {
        // TODO: Add debug
        BuildExpressionCore(builder, isVoid);
    }

    protected abstract void BuildExpressionCore(YabalBuilder builder, bool isVoid);

    public virtual Expression Optimize() => this;

    public abstract bool OverwritesB { get; }

    public abstract LanguageType Type { get; }

    public abstract Expression CloneExpression();
}
