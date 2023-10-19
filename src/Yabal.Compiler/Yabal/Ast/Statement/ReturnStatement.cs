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

        if (Expression is null)
        {
            // ignore
        }
        else if (Expression is InitStructExpression initStruct)
        {
            builder.InitStruct(returnType, builder.ReturnValue, initStruct);
        }
        else if (returnType.Size == 1)
        {
            Expression.BuildExpression(builder, false);
            builder.StoreA(builder.ReturnValue);
        }
        else if (returnType.Size == 0)
        {
            Expression.BuildExpression(builder, false);
        }
        else
        {
            throw new NotImplementedException();
        }

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
