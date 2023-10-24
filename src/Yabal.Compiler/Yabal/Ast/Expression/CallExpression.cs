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
    private InstructionLabel? _returnLabel;
    private IReadOnlyList<Expression>? _arguments;
    private LanguageFunction? _functionType;

    public Function? Function { get; private set; }

    public Pointer? FunctionReference { get; private set; }

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
            Function.MarkUsed();
        }
        else
        {
            Namespace? ns = null;
            Identifier name;

            var argumentTypes = Arguments.Select(i => i.Type).ToList();

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
                if (builder.TryGetVariable(identifier.Name, out var variable) &&
                    variable is { Type: { StaticType: StaticType.Function, FunctionType: {} functionType} })
                {
                    FunctionReference = variable.Pointer;
                    _functionType = functionType;

                    if (argumentTypes.Count != functionType.Parameters.Count)
                    {
                        builder.AddError(ErrorLevel.Error, Range, $"Function reference has {functionType.Parameters.Count} parameters, but {argumentTypes.Count} arguments were provided");
                    }

                    if (!argumentTypes.SequenceEqual(functionType.Parameters))
                    {
                        builder.AddError(ErrorLevel.Error, Range, $"Function reference has parameters {string.Join(", ", functionType.Parameters)}, but arguments were {string.Join(", ", argumentTypes)}");
                    }

                    return;
                }

                name = identifier;
            }
            else
            {
                throw new InvalidCodeException("Callee must be an identifier", Range);
            }

            if (builder.TryGetFunctionExact(Range, ns, name.Name, argumentTypes, out var exactFunction))
            {
                Function = exactFunction;
            }
            else if (TryFindFunction(builder, ns, name, out var result))
            {
                Function = result.function;
                _arguments = result.arguments;
            }
            else if (builder.TryGetSingleFunction(Range, ns, name.Name, out var singleFunction))
            {
                Function = singleFunction;
            }
            else if (builder.TryGetFunctionFuzzy(Range, ns, name.Name, argumentTypes, out var fuzzyFunction))
            {
                Function = fuzzyFunction;
            }
            else
            {
                throw new InvalidCodeException($"Could not find function {name.Name} with {argumentTypes.Count} arguments", Range);
            }

            var count = Math.Min(Arguments.Count, Function.Parameters.Count);

            for (var i = 0; i < count; i++)
            {
                if (Arguments[i] is ITypeExpression typeExpression)
                {
                    typeExpression.Initialize(builder, Function.Parameters[i].Type);
                }
            }

            Function.References.Add(name);
            Function.MarkUsed();
        }

        if (Function.Inline)
        {
            _body = Function.Body.CloneStatement();
            _block = builder.PushBlock();

            if (Function.Block?.Namespace is { } functionNamespace)
            {
                _block.Namespace = functionNamespace;
            }

            _returnLabel = _body is { Statements: [ReturnStatement] } ? null : builder.CreateLabel();
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
        BuildExpressionCore(builder, true);
    }

    private void BuildExpressionCore(YabalBuilder builder, bool loadReturn)
    {
        if (FunctionReference != null)
        {
            builder.Call(FunctionReference, _arguments ?? Arguments, isReference: true);

            if (loadReturn)
            {
                builder.LoadA(builder.ReturnValue);
            }

            return;
        }

        if (Function is null)
        {
            throw new InvalidOperationException("Function not set");
        }

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

            if (_body is { Statements: [ReturnStatement statement]})
            {
                statement.Expression?.BuildExpression(builder, false, Function.ReturnType);
            }
            else
            {
                _body!.Build(builder);
            }

            if (_returnLabel != null)
            {
                builder.Mark(_returnLabel);
            }

            builder.PopBlock();
            builder.ReturnType = previousReturn;
        }
        else
        {
            builder.Call(Function.Label, _arguments ?? Arguments);

            if (loadReturn)
            {
                builder.LoadA(builder.ReturnValue);
            }
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (Function is { Inline: true })
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

            if (_body is { Statements: [ReturnStatement statement]})
            {
                statement.Expression?.BuildExpressionToPointer(builder, Function.ReturnType, pointer);
            }
            else
            {
                _body!.Build(builder);
            }

            if (_returnLabel != null)
            {
                builder.Mark(_returnLabel);
            }

            builder.PopBlock();
            builder.ReturnValue = previousPointer;
            builder.ReturnType = previousReturn;
        }
        else
        {
            BuildExpressionCore(builder, false);

            for (var i = 0; i < suggestedType.Size; i++)
            {
                builder.ReturnValue.CopyTo(builder, pointer, i);
            }
        }
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => _functionType?.ReturnType ?? Function?.ReturnType ?? LanguageType.Unknown;

    public override Expression CloneExpression()
    {
        return new CallExpression(
            Range,
            Callee.IsLeft ? Callee.Left.CloneExpression() : Callee.Right,
            (_arguments ?? Arguments).Select(x => x.CloneExpression()).ToList()
        )
        {
            _functionType = _functionType,
            FunctionReference = FunctionReference,
        };
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        return new CallExpression(
            Range,
            Callee.IsLeft ? Callee.Left.Optimize(suggestedType) : Callee.Right,
            (_arguments ?? Arguments).Select(x => x.Optimize(suggestedType)).ToList()
        )
        {
            _functionType = _functionType,
            FunctionReference = FunctionReference,
            _block = _block,
            _variables = _variables,
            _body = _body?.Optimize(),
            Function = Function,
            _returnLabel = _returnLabel
        };
    }
}
