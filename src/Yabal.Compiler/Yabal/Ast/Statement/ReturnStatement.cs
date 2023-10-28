using Yabal.Exceptions;

namespace Yabal.Ast;

public record ReturnStatement(SourceRange Range, Expression? Expression) : Statement(Range)
{
    public LanguageType? ReturnType { get; set; }

    public override void Initialize(YabalBuilder builder)
    {
        Expression?.Initialize(builder);
        ReturnType = builder.ReturnType;

        if (ReturnType != null && Expression is ITypeExpression typeExpression)
        {
            typeExpression.Initialize(builder, ReturnType);
        }
    }

    public override void Build(YabalBuilder builder)
    {
        if (builder.Block.Return == null)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ReturnOutsideFunction);
        }

        var returnType = builder.ReturnType ?? throw new InvalidCodeException("Cannot return outside of a function", Range);

        Expression?.BuildExpressionToPointer(builder, returnType, builder.ReturnValue);

        if (builder.Block.Return != null)
        {
            builder.Jump(builder.Block.Return);
        }
    }

    public override Statement CloneStatement()
    {
        return new ReturnStatement(Range, Expression?.CloneExpression())
        {
            ReturnType = ReturnType
        };
    }

    public override Statement Optimize()
    {
        return new ReturnStatement(Range, Expression?.Optimize(ReturnType))
        {
            ReturnType = ReturnType
        };
    }
}
