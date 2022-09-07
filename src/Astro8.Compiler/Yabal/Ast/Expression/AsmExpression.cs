using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AsmArgument;

public record AsmVariable(string Name) : AsmArgument;

public record AsmInteger(int Value) : AsmArgument;

public record AsmLabel(string Name) : AsmArgument;

public record AsmInstruction(SourceRange Range, string Name, AsmArgument? Argument) : AsmStatement(Range);

public record AsmDefineLabel(SourceRange Range, string Name) : AsmStatement(Range);

public record AsmRawValue(SourceRange Range, AsmArgument Value) : AsmStatement(Range);

public record AsmStatement(SourceRange Range);

public record AsmExpression(SourceRange Range, List<AsmStatement> Statements) : Expression(Range)
{
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

                    if (!argValue.HasValue)
                    {
                        builder.Emit(name);
                    }
                    else if (instruction.MicroInstructions.Any(mi => mi.IsIR))
                    {
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
    }

    public override bool OverwritesB => true;

    public override LanguageType Type => LanguageType.Assembly;

    public override Expression CloneExpression()
    {
        return this;
    }
}
