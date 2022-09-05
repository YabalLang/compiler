using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record CallExpression(
    SourceRange Range,
    Expression Callee,
    List<Expression> Arguments
) : Expression(Range)
{
    public override void BeforeBuild(YabalBuilder builder)
    {
        Callee.BeforeBuild(builder);

        foreach (var argument in Arguments)
        {
            argument.BeforeBuild(builder);
        }
    }

    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        if (Callee is not IdentifierExpression id)
        {
            builder.AddError(ErrorLevel.Error, Callee.Range, ErrorMessages.CallExpressionCalleeMustBeIdentifier);
            return LanguageType.Void;
        }

        var function = builder.GetFunction(id.Name);

        var argumentTypes = builder.Call(function.Label, Arguments);

        if (Arguments.Count != function.Parameters.Count)
        {
            builder.AddError(ErrorLevel.Error, Range, ErrorMessages.CallExpressionArgumentCountMismatch(id.Name, function.Parameters.Count, Arguments.Count));
        }

        var amount = Math.Min(Arguments.Count, function.Parameters.Count);

        for (var i = 0; i < amount; i++)
        {
            if (argumentTypes[i] != function.Parameters[i].Type)
            {
                builder.AddError(ErrorLevel.Error, Range, ErrorMessages.ArgumentTypeMismatch(i, id.Name, function.Parameters[i].Type, argumentTypes[i]));
            }
        }

        return function.ReturnType;
    }

    public override bool OverwritesB => true;
}
