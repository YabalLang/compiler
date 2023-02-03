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
    private InstructionLabel _returnLabel = null!;

    public Function Function { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        if (Callee is not IdentifierExpression { Identifier: var identifier})
        {
            throw new NotSupportedException("Callee must be an identifier");
        }

        Function = builder.GetFunction(identifier.Name);
        Function.References.Add(this);

        foreach (var argument in Arguments)
        {
            argument.Initialize(builder);
        }

        if (Function.Inline)
        {
            _body = Function.Body.CloneStatement();
            _block = builder.PushBlock();

            _returnLabel = builder.CreateLabel();
            _block.Return = _returnLabel;

            _variables = new (Variable, Expression)[Arguments.Count];

            // Copy variables from parent blocks
            if (Function.Block is { } functionBlock)
            {
                var current = functionBlock;

                while (current != null)
                {
                    foreach (var variable in current.Variables)
                    {
                        if (_block.TryGetVariable(variable.Key, out _))
                        {
                            continue;
                        }

                        _block.DeclareVariable(variable.Key, variable.Value);
                    }

                    current = current.Parent;
                }
            }

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
            var previousReturn = builder.ReturnType;

            builder.ReturnType = Function.ReturnType;
            builder.PushBlock(_block);

            foreach (var (variable, expression) in _variables)
            {
                if (!variable.CanBeRemoved)
                {
                    builder.SetValue(variable.Pointer, variable.Type, expression);
                }
            }

            _body!.Build(builder);
            builder.Mark(_returnLabel);

            builder.PopBlock();
            builder.ReturnType = previousReturn;
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
            Function = Function,
            _returnLabel = _returnLabel
        };
    }
}
