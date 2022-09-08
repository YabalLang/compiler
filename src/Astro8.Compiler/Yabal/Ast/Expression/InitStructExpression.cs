using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record InitStructItem(string? Name, Expression Value);

public record InitStructExpression(SourceRange Range, List<InitStructItem> Items, LanguageType? StructType) : Expression(Range)
{
    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        throw new InvalidOperationException();
    }

    public override bool OverwritesB => false;

    public override LanguageType Type => StructType ?? throw new InvalidOperationException();

    public override Expression CloneExpression()
    {
        return new InitStructExpression(
            Range,
            Items.Select(i => i with { Value = i.Value.CloneExpression() }).ToList(),
            StructType
        );
    }

    public override Expression Optimize()
    {
        return new InitStructExpression(
            Range,
            Items.Select(i => i with { Value = i.Value.Optimize() }).ToList(),
            StructType
        );
    }
}
