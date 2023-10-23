using Yabal.Exceptions;
using Yabal.Instructions;
using Yabal.Visitor;

namespace Yabal.Ast;

public record CallExpression(
    SourceRange Range,
    Either<Expression, Function> Callee,
    List<Expression> Arguments
) : Expression(Range)
{
    private BlockStack _block = null!;
    private (Variable, Expression)[] _variables = null!;
    private BlockStatement? _body;
    private InstructionLabel _returnLabel = null!;
    private IReadOnlyList<Expression>? _arguments;

    public Function Function { get; private set; } = null!;

    public override void Initialize(YabalBuilder builder)
    {
        _arguments = Arguments;

        foreach (var argument in Arguments)
        {
            argument.Initialize(builder);
        }

        if (Callee.IsRight)
        {
            Function = Callee.Right;
        }
        else
        {
            Namespace? ns = null;
            Identifier name;

            if (Callee.Left is MemberExpression memberExpression)
            {
                var items = new List<string>();
                var current = memberExpression.Expression;

                while (current is MemberExpression { Expression: var expression, Name: var part })
                {
                    items.Add(part.Name);
                    current = expression;
                }

                if (current is IdentifierExpression { Identifier: var identifier })
                {
                    items.Add(identifier.Name);
                }
                else
                {
                    throw new InvalidCodeException("Callee must be an identifier", Range);
                }

                items.Reverse();
                ns = new Namespace(items);
                name = memberExpression.Name;
            }
            else if (Callee.Left is IdentifierExpression { Identifier: var identifier })
            {
                name = identifier;
            }
            else
            {
                throw new InvalidCodeException("Callee must be an identifier", Range);
            }

            var argumentTypes = Arguments.Select(i => i.Type).ToArray();

            if (builder.TryGetFunctionExact(Range, ns, name.Name, argumentTypes, out var exactFunction))
            {
                Function = exactFunction;
            }
            else if (TryFindFunction(builder, ns, name, out var result))
            {
                Function = result.function;
                _arguments = result.arguments;
            }
            else if (builder.TryGetFunctionFuzzy(Range, ns, name.Name, argumentTypes, out var fuzzyFunction))
            {
                Function = fuzzyFunction;
            }
            else
            {
                throw new InvalidCodeException($"Could not find function {name.Name} with {argumentTypes.Length} arguments", Range);
            }
        }

        Function.References.Add(this);

        if (Function.Inline)
        {
            _body = Function.Body.CloneStatement();
            _block = builder.PushBlock();

            if (Function.Block?.Namespace is { } functionNamespace)
            {
                _block.Namespace = functionNamespace;
            }

            _returnLabel = builder.CreateLabel();
            _block.Return = _returnLabel;

            _variables = new (Variable, Expression)[_arguments.Count];

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

            for (var i = 0; i < _arguments.Count; i++)
            {
                var parameter = Function.Parameters[i];
                var expression = _arguments[i];
                var variable = builder.CreateVariable(parameter.Name, parameter.Type, expression);
                _variables[i] = (variable, expression);
            }

            _body.Initialize(builder);
            builder.PopBlock();
        }
    }

    private bool TryFindFunction(YabalBuilder builder, Namespace? ns, Identifier name, out (Function function, Expression[] arguments) result)
    {
        var castedArguments = new Expression[Arguments.Count];
        var castExpressions = new List<Expression>();

        foreach (var function in builder.GetFunctions(ns, name.Name))
        {
            if (function.Parameters.Count > castedArguments.Length)
            {
                continue;
            }

            castExpressions.Clear();

            var isMatch = true;

            for (var i = 0; i < Arguments.Count; i++)
            {
                var argumentType = Arguments[i].Type;
                var parameterType = function.Parameters[i].Type;

                if (argumentType.Equals(parameterType))
                {
                    castedArguments[i] = Arguments[i];
                }
                else if (builder.CastOperators.TryGetValue((argumentType, parameterType), out var caster))
                {
                    var castExpression = new CallExpression(
                        Arguments[i].Range,
                        caster,
                        new List<Expression> { Arguments[i] }
                    );

                    castExpressions.Add(castExpression);
                    castedArguments[i] = castExpression;
                }
                else
                {
                    isMatch = false;
                    break;
                }
            }

            if (!isMatch)
            {
                continue;
            }

            foreach (var castExpression in castExpressions)
            {
                castExpression.Initialize(builder);
            }

            result = (function, castedArguments);
            return true;
        }

        result = default;
        return false;
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
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
            builder.Call(Function.Label, _arguments ?? Arguments);
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (Function.Inline)
        {
            var previousReturn = builder.ReturnType;
            var previousPointer = builder.ReturnValue;

            builder.ReturnType = Function.ReturnType;
            builder.ReturnValue = pointer;
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
            builder.ReturnValue = previousPointer;
            builder.ReturnType = previousReturn;
        }
        else
        {
            BuildExpression(builder, false, suggestedType);

            for (var i = 0; i < suggestedType.Size; i++)
            {
                builder.ReturnValue.CopyTo(builder, pointer, i);
            }
        }
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => Function.ReturnType;

    public override Expression CloneExpression()
    {
        return new CallExpression(
            Range,
            Callee.IsLeft ? Callee.Left.CloneExpression() : Callee.Right,
            (_arguments ?? Arguments).Select(x => x.CloneExpression()).ToList()
        );
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new CallExpression(
            Range,
            Callee.IsLeft ? Callee.Left.Optimize(suggestedType) : Callee.Right,
            (_arguments ?? Arguments).Select(x => x.Optimize(suggestedType)).ToList()
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
