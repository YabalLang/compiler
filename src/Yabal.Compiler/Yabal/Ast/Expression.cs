using Yabal.Instructions;

namespace Yabal.Ast;

public interface ITypeExpression
{
    void Initialize(YabalBuilder builder, LanguageType type);
}

public abstract record Expression(SourceRange Range) : Node(Range), IExpression
{
    public void BuildExpression(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        // TODO: Add debug
        BuildExpressionCore(builder, isVoid, suggestedType);
    }

    public abstract void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer);

    protected abstract void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType);

    public virtual Expression Optimize(LanguageType? suggestedType) => this;

    public abstract bool OverwritesB { get; }

    public abstract LanguageType Type { get; }

    public abstract Expression CloneExpression();
}
