using Astro8.Instructions;
using Astro8.Yabal.Visitor;

namespace Astro8.Yabal.Ast;

public record CallExpression(
    SourceRange Range,
    Expression Callee,
    List<Expression> Arguments
) : Expression(Range)
{
    private BlockStack _block = null!;
    private (Variable, Expression)[] _variables = null!;
    private BlockStatement? _body;

    public Function Function { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        if (Callee is not IdentifierExpression identifier)
        {
            throw new NotSupportedException("Callee must be an identifier");
        }

        Function = builder.GetFunction(identifier.Name);

        foreach (var argument in Arguments)
        {
            argument.Initialize(builder);
        }

        if (Function.Inline)
        {
            _body = Function.Body.CloneStatement();
            _block = builder.PushBlock();

            _variables = new (Variable, Expression)[Arguments.Count];

            for (var i = 0; i < Arguments.Count; i++)
            {
                var parameter = Function.Parameters[i];
                var expression = Arguments[i];
                var variable = builder.CreateVariable(parameter.Name, parameter.Type, expression);
                _variables[i] = (variable, expression);
            }

            _body.Initialize(builder);
            builder.PopBlock();
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        if (Function.Inline)
        {
            builder.PushBlock(_block);

            foreach (var (variable, expression) in _variables)
            {
                if (!variable.CanBeRemoved)
                {
                    builder.SetValue(variable.Pointer, variable.Type, expression);
                }
            }

            _body!.Build(builder);

            builder.PopBlock();
        }
        else
        {
            builder.Call(Function.Label, Arguments);
        }
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Function.ReturnType;

    public override Expression CloneExpression()
    {
        return new CallExpression(
            Range,
            Callee.CloneExpression(),
            Arguments.Select(x => x.CloneExpression()).ToList()
        );
    }

    public override Expression Optimize()
    {
        return new CallExpression(
            Range,
            Callee.Optimize(),
            Arguments.Select(x => x.Optimize()).ToList()
        )
        {
            _block = _block,
            _variables = _variables,
            _body = _body?.Optimize(),
            Function = Function
        };
    }
}
