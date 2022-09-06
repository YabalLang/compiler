using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record SwitchItem(List<Expression> Cases, Expression Value);

public record SwitchExpression(SourceRange Range, Expression Value, List<SwitchItem> Items, Expression Default) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Value.Initialize(builder);

        foreach (var item in Items)
        {
            foreach (var @case in item.Cases)
            {
                @case.Initialize(builder);
            }

            item.Value.Initialize(builder);
        }

        Default.Initialize(builder);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        Value.BuildExpression(builder, isVoid);

        using var tempValue = builder.GetTemporaryVariable();
        builder.StoreA(tempValue);

        var end = builder.CreateLabel();

        foreach (var item in Items)
        {
            var returnValue = builder.CreateLabel();
            var next = builder.CreateLabel();

            foreach (var caseValue in item.Cases)
            {
                caseValue.BuildExpression(builder, isVoid);
                builder.LoadB(tempValue);
                builder.Sub();
                builder.JumpIfZero(returnValue);
                builder.Jump(next);
            }

            builder.Mark(returnValue);
            item.Value.BuildExpression(builder, isVoid);
            builder.Jump(end);
            builder.Mark(next);
        }

        Default.BuildExpression(builder, isVoid);
        builder.Mark(end);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Default?.Type ?? Items[0].Value.Type;
}
