using Yabal.Instructions;

namespace Yabal.Ast;

public record AsmArgument;

public record AsmVariable(string Name) : AsmArgument;

public record AsmInteger(int Value) : AsmArgument;

public record AsmLabel(string Name) : AsmArgument;

public record AsmInstruction(SourceRange Range, string Name, AsmArgument? Value) : AsmStatement(Range), IAsmArgument;

public record AsmDefineLabel(SourceRange Range, string Name) : AsmStatement(Range);

public record AsmRawValue(SourceRange Range, AsmArgument Value) : AsmStatement(Range), IAsmArgument;

public abstract record AsmStatement(SourceRange Range);

public interface IAsmArgument
{
    AsmArgument? Value { get; }
}

public record AsmExpression(SourceRange Range, List<AsmStatement> Statements) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        foreach (var statement in Statements.OfType<IAsmArgument>())
        {
            if (statement.Value is AsmVariable value && builder.TryGetVariable(value.Name, out var variable))
            {
                variable.Constant = false;
            }
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid)
    {
        var labels = new Dictionary<string, InstructionPointer>();

        InstructionPointer GetLabel(string name)
        {
            if (!labels.TryGetValue(name, out var label))
            {
                label = builder.CreateLabel(name);
                labels.Add(name, label);
            }

            return label;
        }

        PointerOrData? GetPointerOrData(AsmArgument? asmArgument)
        {
            switch (asmArgument)
            {
                case AsmVariable {Name: var variableName}:
                    if (!builder.TryGetVariable(variableName, out var variable))
                    {
                        builder.AddError(ErrorLevel.Error, Range, ErrorMessages.UndefinedVariable(variableName));
                        return 0;
                    }

                    return variable.Pointer;
                case AsmLabel {Name: var labelName}:
                    return GetLabel(labelName);
                case AsmInteger {Value: var intValue}:
                    return intValue;
                default:
                    return null;
            }
        }

        var bank = 0;
        SourceRange? lastBankSwitch = null;

        foreach (var statement in Statements)
        {
            switch (statement)
            {
                case AsmRawValue {Value: var value}:
                    builder.EmitRaw(GetPointerOrData(value) ?? 0);
                    break;
                case AsmDefineLabel { Name: var name }:
                    builder.Mark(GetLabel(name));
                    break;
                case AsmInstruction(var range, var nameValue, var argument):
                {
                    var name = nameValue.ToUpperInvariant();
                    var instruction = Instruction.Default.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    var argValue = GetPointerOrData(argument);

                    BinaryOperator? binaryOperator = name switch
                    {
                        "JE" => BinaryOperator.Equal,
                        "JNE" => BinaryOperator.NotEqual,
                        "JL" => BinaryOperator.LessThan,
                        "JLE" => BinaryOperator.LessThanOrEqual,
                        "JG" => BinaryOperator.GreaterThan,
                        "JGE" => BinaryOperator.GreaterThanOrEqual,
                        _ => null
                    };

                    if (binaryOperator.HasValue)
                    {
                        var skipLabel = builder.CreateLabel();

                        if (!argValue.HasValue)
                        {
                            builder.AddError(ErrorLevel.Error, range, ErrorMessages.BinaryInstructionRequiresLabel);
                            argValue = skipLabel;
                        }

                        BinaryExpression.Jump(binaryOperator.Value, builder, skipLabel, argValue.Value);
                        builder.Mark(skipLabel);
                        continue;
                    }

                    if (instruction == null)
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.UnknownInstruction(name));
                        continue;
                    }

                    if (bank != 0 &&
                        instruction.MicroInstructions.Skip(4).Any(mi => mi.IsCR && mi.IsAW) &&
                        instruction.MicroInstructions.Skip(4).Any(mi => mi.IsRM))
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.InstructionBankSwitched);
                    }

                    if (!argValue.HasValue)
                    {
                        builder.Emit(name);
                        continue;
                    }

                    if (argValue.Value.IsLeft)
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.PointerBankSwitched);
                    }

                    if (instruction.MicroInstructions.Any(mi => mi.IsIR))
                    {
                        if (name == "BNK")
                        {
                            bank = argValue.Value.IsRight ? argValue.Value.Right : 0;
                            lastBankSwitch = range;
                        }

                        builder.Emit(name, argValue.Value);
                    }
                    else
                    {
                        builder.Emit(name);
                        builder.EmitRaw(argValue.Value);
                    }

                    break;
                }
            }
        }

        if (bank != 0)
        {
            builder.AddError(ErrorLevel.Warning, lastBankSwitch ?? Range, ErrorMessages.BankNotSwitchedBack);
        }
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => LanguageType.Assembly;

    public override Expression CloneExpression()
    {
        return this;
    }
}
