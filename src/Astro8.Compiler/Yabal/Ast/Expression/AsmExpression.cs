using Astro8.Instructions;

namespace Astro8.Yabal.Ast;

public record AsmArgument;
public record AsmVariable(string Name) : AsmArgument;
public record AsmInteger(int Value) : AsmArgument;

public record AsmInstruction(string Name, AsmArgument? Argument);

public record AsmExpression(SourceRange Range, List<AsmInstruction> Instructions) : Expression(Range)
{
    public override LanguageType BuildExpression(YabalBuilder builder, bool isVoid)
    {
        foreach (var (name, argument) in Instructions)
        {
            var instruction = Instruction.Default.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (instruction == null)
            {
                throw new KeyNotFoundException($"Unknown instruction '{name}'");
            }

            var isBusValue = instruction.MicroInstructions.Any(mi => mi.IsIR);

            switch (argument)
            {
                // Bus value
                case AsmVariable { Name: var variableName } when isBusValue:
                    builder.Emit(name, builder.GetVariable(variableName).Pointer);
                    break;
                case AsmInteger { Value: var intValue } when isBusValue:
                    builder.Emit(name, intValue);
                    break;

                // Raw value
                case AsmVariable { Name: var variableName }:
                    builder.EmitRaw(builder.GetVariable(variableName).Pointer);
                    builder.Emit(name);
                    break;
                case AsmInteger { Value: var intValue }:
                    builder.EmitRaw(intValue);
                    builder.Emit(name);
                    break;

                // No argument
                default:
                    builder.Emit(name);
                    break;
            }
        }

        return LanguageType.Integer;
    }
}
