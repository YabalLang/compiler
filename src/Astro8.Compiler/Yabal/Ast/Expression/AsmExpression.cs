using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AsmArgument;

public record AsmVariable(string Name) : AsmArgument;

public record AsmInteger(int Value) : AsmArgument;

public record AsmLabel(string Name) : AsmArgument;

public record AsmInstruction(string Name, AsmArgument? Argument) : IAsmStatement;

public record AsmDefineLabel(string Name) : IAsmStatement;

public record AsmRawValue(AsmArgument Value) : IAsmStatement;

public interface IAsmStatement
{
}

public record AsmExpression(SourceRange Range, List<IAsmStatement> Statements) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
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
            return asmArgument switch
            {
                AsmVariable {Name: var variableName} => builder.GetVariable(variableName).Pointer,
                AsmLabel {Name: var labelName} => GetLabel(labelName),
                AsmInteger {Value: var intValue} => intValue,
                _ => null
            };
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
                case AsmInstruction(var nameValue, var argument):
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
                        if (!argValue.HasValue)
                        {
                            throw new InvalidOperationException($"Binary operator '{name}' requires an argument");
                        }

                        var skipLabel = builder.CreateLabel();
                        BinaryExpression.Jump(binaryOperator.Value, builder, skipLabel, argValue.Value);
                        builder.Mark(skipLabel);
                        continue;
                    }

                    if (instruction == null)
                    {
                        throw new KeyNotFoundException($"Unknown instruction '{name}'");
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

        return LanguageType.Assembly;
    }

    public override bool OverwritesB => true;
}
