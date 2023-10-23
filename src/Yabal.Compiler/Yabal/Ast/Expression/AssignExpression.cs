﻿namespace Yabal.Ast;

public record AssignExpression(SourceRange Range, AssignableExpression Object, Expression Value) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Object.Initialize(builder);
        Value.Initialize(builder);

        Object.MarkModified();
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        Object.BuildExpressionToPointer(builder, suggestedType, pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        Object.Assign(builder, Value, Range);
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Value.Type;

    public override Expression CloneExpression()
    {
        return new AssignExpression(Range, Object.CloneExpression(), Value.CloneExpression());
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new AssignExpression(Range, Object, Value.Optimize(suggestedType ?? Object.Type));
    }
}
