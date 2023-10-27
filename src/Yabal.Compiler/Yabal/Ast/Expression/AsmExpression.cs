using Yabal.Instructions;

namespace Yabal.Ast;

public record AsmArgument;

public record AsmVariable(Identifier Identifier) : AsmArgument;

public record AsmInteger(int Value) : AsmArgument;

public record AsmLabel(string Name) : AsmArgument;

public record AsmInstruction(SourceRange Range, string Name, AsmArgument? FirstValue, AsmArgument? SecondValue) : AsmStatement(Range), IAsmArgument;

public record AsmComment(SourceRange Range, string Text) : AsmStatement(Range);

public record AsmDefineLabel(SourceRange Range, string Name) : AsmStatement(Range);

public record AsmRawValue(SourceRange Range, AsmArgument FirstValue) : AsmStatement(Range), IAsmArgument
{
    public AsmArgument? SecondValue => null;
}

public abstract record AsmStatement(SourceRange Range);

public interface IAsmArgument
{
    AsmArgument? FirstValue { get; }

    AsmArgument? SecondValue { get; }
}

public record AsmExpression(SourceRange Range, List<AsmStatement> Statements) : Expression(Range)
{
    public override void Initialize(YabalBuilder builder)
    {
        foreach (var statement in Statements.OfType<IAsmArgument>())
        {
            if (statement.FirstValue is AsmVariable firstValue && builder.TryGetVariable(firstValue.Identifier.Name, out var variable))
            {
                variable.AddReference(firstValue.Identifier);
            }


            if (statement.SecondValue is AsmVariable secondValue && builder.TryGetVariable(secondValue.Identifier.Name, out variable))
            {
                variable.AddReference(secondValue.Identifier);
            }
        }
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        BuildExpressionCore(builder, false, suggestedType);
        builder.StoreA(pointer);
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
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
                case AsmVariable {Identifier: var identifier}:
                    if (!builder.TryGetVariable(identifier.Name, out var variable))
                    {
                        builder.AddError(ErrorLevel.Error, Range, ErrorMessages.UndefinedVariable(identifier.Name));
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
                case AsmRawValue {FirstValue: var value}:
                    builder.EmitRaw(GetPointerOrData(value) ?? 0);
                    break;
                case AsmDefineLabel { Name: var name }:
                    builder.Mark(GetLabel(name));
                    break;
                case AsmComment { Text: var text }:
                    builder.EmitComment(text);
                    break;
                case AsmInstruction(var range, var nameValue, var firstValue, var secondValue):
                {
                    var name = nameValue.ToUpperInvariant();
                    var instruction = Instruction.Default.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    var argFirstValue = GetPointerOrData(firstValue);
                    var argSecondValue = GetPointerOrData(secondValue);

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

                        if (!argFirstValue.HasValue)
                        {
                            builder.AddError(ErrorLevel.Error, range, ErrorMessages.BinaryInstructionRequiresLabel);
                            argFirstValue = skipLabel;
                        }

                        BinaryExpression.Jump(binaryOperator.Value, builder, skipLabel, argFirstValue.Value);
                        builder.Mark(skipLabel);
                        continue;
                    }

                    if (instruction == null)
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.UnknownInstruction(name));
                        continue;
                    }

                    var readsInstructionData = instruction.MicroInstructions.Any(mi => mi.IsIR);
                    var switchesBank = instruction.MicroInstructions.Any(mi => mi.IsIR && mi.IsBNK);
                    var readsLargeValue = instruction.MicroInstructions.Skip(4).Any(mi => mi.IsCR && mi.IsAW) &&
                                          instruction.MicroInstructions.Skip(4).Any(mi => mi.IsRM);

                    if (switchesBank)
                    {
                        bank = (argFirstValue?.IsRight ?? false) ? argFirstValue.Value.Right : 0;
                        lastBankSwitch = range;
                    }
                    else if (bank != 0 && readsLargeValue)
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.InstructionBankSwitched);
                    }

                    if (!argFirstValue.HasValue)
                    {
                        builder.Emit(name);
                        continue;
                    }

                    if (bank > 0 && argFirstValue.Value.IsLeft)
                    {
                        builder.AddError(ErrorLevel.Error, range, ErrorMessages.PointerBankSwitched);
                    }

                    if (readsInstructionData && readsLargeValue)
                    {
                        builder.Emit(name, argFirstValue.Value);

                        if (argSecondValue is not null)
                        {
                            builder.EmitRaw(argSecondValue.Value);
                        }
                    }
                    else if (readsInstructionData)
                    {
                        builder.Emit(name, argFirstValue.Value);
                    }
                    else
                    {
                        builder.Emit(name);
                        builder.EmitRaw(argFirstValue.Value);
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
