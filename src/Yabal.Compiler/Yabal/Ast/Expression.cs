using Yabal.Instructions;

namespace Yabal.Ast;

public abstract record Expression(SourceRange Range) : Node(Range), IExpression
{
    public void BuildExpression(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        // TODO: Add debug
        BuildExpressionCore(builder, isVoid, suggestedType);
    }

    public virtual void BuildExpression(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpression(builder, false, suggestedType);
        builder.SetValue(pointer, Type, this);
    }

    protected abstract void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType);

    public virtual Expression Optimize() => this;

    public abstract bool OverwritesB { get; }

    public abstract LanguageType Type { get; }

    public abstract Expression CloneExpression();
}
