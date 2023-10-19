using Yabal.Exceptions;

namespace Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression? Expression) : Statement(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        Expression?.Initialize(builder);

        if (builder.Block.Return == null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ReturnOutsideFunction);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        var returnType = builder.ReturnType ?? throw new InvalidCodeException("Cannot return outside of a function", Range);

        Expression?.BuildExpression(builder, returnType, builder.ReturnValue);

        if (builder.Block.Return != null)
        {
            builder.Jump(builder.Block.Return);
        }
    }

    public override Statement CloneStatement()
    {
        return new ReturnStatement(Range, Expression?.CloneExpression());
    }

    public override Statement Optimize()
    {
        return new ReturnStatement(Range, Expression?.Optimize());
    }
}
