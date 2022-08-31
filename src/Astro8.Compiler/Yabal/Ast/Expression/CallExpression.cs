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
            throw new NotSupportedException();
        }

        var function = builder.GetFunction(id.Name);

        var argumentTypes = builder.Call(function.Label, Arguments);

        if (Arguments.Count != function.Parameters.Count)
        {
            throw new InvalidOperationException($"Function {id.Name} expects {function.Parameters.Count} arguments, but {Arguments.Count} were provided.");
        }

        var amount = Math.Min(Arguments.Count, function.Parameters.Count);

        for (var i = 0; i < amount; i++)
        {
            if (argumentTypes[i] != function.Parameters[i].Type)
            {
                throw new InvalidOperationException($"Argument {i} of function {id.Name} is of type {argumentTypes[i]}, but expected {function.Parameters[i].Type}.");
            }
        }

        return function.ReturnType;
    }

    public override bool OverwritesB => true;
}
