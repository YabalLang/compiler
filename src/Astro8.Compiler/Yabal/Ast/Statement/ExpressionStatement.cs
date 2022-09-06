﻿using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record ExpressionStatement(SourceRange Range, Expression Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression.Initialize(builder);
    }

    public override void Build(YabalBuilder builder)
    {
        Expression.BuildExpression(builder, true);
    }
}
