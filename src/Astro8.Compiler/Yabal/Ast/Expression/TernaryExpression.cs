using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record TernaryExpression(SourceRange Range, Expression Expression, Expression Consequent, Expression Alternate) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
        Consequent.Initialize(builder);
        Alternate.Initialize(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        var consequentLabel = builder.CreateLabel();
        var alternateLabel = builder.CreateLabel();
        var end = builder.CreateLabel();
        var expression = Expression.Optimize();

        expression.CreateComparison(builder, alternateLabel, consequentLabel);

        builder.Mark(consequentLabel);
        Consequent.BuildExpression(builder, isVoid);

        builder.Jump(end);
        builder.Mark(alternateLabel);

        Alternate.BuildExpression(builder, isVoid);
        builder.Mark(end);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Consequent.Type;
}
