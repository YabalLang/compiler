using System.Globalization;
using Yabal;

namespace Yabal.Instructions;

public record Instruction(
    int Id,
    string Name,
    params MicroInstruction[] MicroInstructions
)
{
    public static implicit operator int(Instruction definition) => definition.Id;

    private static readonly string[] Flags = {
        "ZEROFLAG",
        "CARRYFLAG"
    };

    private static readonly int FlagLength = 1 << Flags.Length;

    private static Instruction Parse(string value, int id, Instruction? parent = null)
    {
        const int microInstructionLength = 32;
        Span<bool?> flags = stackalloc bool?[Flags.Length];
        Span<bool> definedOffsets = stackalloc bool[microInstructionLength];

        if (!value.AsSpan().TrySplit('(', out var nameSpan, out var definition))
        {
            throw new FormatException($"No name found in instruction index {id}");
        }

        var name = nameSpan.Trim().ToString();
        var microInstructions = new MicroInstruction[microInstructionLength];

        for (var i = 0; i < microInstructions.Length; i++)
        {
            microInstructions[i] = MicroInstruction.None;
        }

        foreach(var microInstructionLine in definition.Split('&'))
        {
            // Get offset
            if (!microInstructionLine.TrySplit('=', out var offsetSpan, out var line))
            {
                throw new FormatException($"Expected index and micro instructions in instruction {name}");
            }

#if NETSTANDARD2_0
            var offsetStr = offsetSpan.Trim().ToString();

            if (!int.TryParse(offsetStr, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                throw new FormatException($"Invalid offset '{offsetStr}' in instruction {name}");
            }
#else
            if (!int.TryParse(offsetSpan.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                throw new FormatException($"Invalid offset '{offsetSpan}' in instruction {name}");
            }
#endif

            var offset = index * FlagLength;

            // Parse flags
            flags.Clear();

            if (line.TrySplit('|', out var left, out var flagsSpan))
            {
                foreach (var flag in flagsSpan.Split(','))
                {
                    var flagName = flag.Trim();
                    var flagValue = true;

                    if (flag[0] == '!')
                    {
                        flagName = flagName.Slice(1);
                        flagValue = false;
                    }

                    var flagIndex = Flags.IndexOf(flagName);

                    if (flagIndex == -1)
                    {
                        throw new FormatException($"Invalid flag '{flagName.ToString()}' in instruction {name}");
                    }

                    flags[flagIndex] = flagValue;
                }

                line = left;
            }

            // Add micro instructions
            foreach (var code in line.Split(','))
            {
                if (!MicroInstruction.All.TryGetValue(code.Trim(), StringComparison.OrdinalIgnoreCase, out var microInstruction))
                {
                    throw new FormatException($"Invalid microinstruction: {code.ToString()}");
                }

                for (var i = 0; i < FlagLength; i++)
                {
                    if (flags.HasFlag(i))
                    {
                        microInstructions[offset + i] |= microInstruction;
                        definedOffsets[offset + i] = true;
                    }
                }
            }
        }

        // Copy parent micro instructions
        if (parent != null)
        {
            var parentMicroInstructions = parent.MicroInstructions.AsSpan();

            for (var i = 0; i < definedOffsets.Length; i++)
            {
                if (!definedOffsets[i])
                {
                    microInstructions[i] = parentMicroInstructions[i];
                }
            }
        }

        return new Instruction(
            id,
            name.ToUpperInvariant(),
            microInstructions
        );
    }

    public static IReadOnlyList<Instruction> Parse(string[] lines)
    {
        var result = new Instruction[lines.Length];

        Instruction? root = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var instruction = Parse(lines[i], i, root);

            if (i == 0)
            {
                root = instruction;
            }

            result[i] = instruction;
        }

        return result;
    }

    private static readonly string[] DefaultInstructions =
	{
		"fetch( 0=cr,aw & 1=rm,iw,ce & 2=ei", // Fetch
		"ain( 2=aw,ir & 3=wa,rm & 4=ei", // LoadA
		"bin( 2=aw,ir & 3=wb,rm & 4=ei", // LoadB
		"cin( 2=aw,ir & 3=wc,rm & 4=ei", // LoadC
		"ldia( 2=wa,ir & 3=ei", // Load immediate A <val>
		"ldib( 2=wb,ir & 3=ei", // Load immediate B <val>
		//"rdexp( 2=ir,exi & 3=wa,re & 4=ei", // Read from expansion port <index> to register A
		//"wrexp( 2=ir,exi & 3=ra,we & 4=ei", // Write from reg A to expansion port <index>
		"sta( 2=aw,ir & 3=ra,wm & 4=ei", // Store A <addr>
		"add( 2=wa,eo,fl & 3=ei", // Add
		"sub( 2=wa,eo,su,fl & 3=ei", // Subtract
		"mult( 2=wa,eo,mu,fl & 3=ei", // Multiply
		"div( 2=wa,eo,di,fl & 3=ei", // Divide
		"jmp( 2=cr,aw & 3=rm,j & 4=ei", // Jump to address following instruction
		"jmpz( 2=cr,aw & 3=ce,rm & 4=j | zeroflag & 5=ei", // Jump if zero to address following instruction
		"jmpc( 2=cr,aw & 3=ce,rm & 4=j | carryflag & 5=ei", // Jump if carry to address following instruction
		"jreg( 2=ra,j & 3=ei", // Jump to the address stored in Reg A
		"ldain( 2=ra,aw & 3=wa,rm & 4=ei", // Use reg A as memory address, then copy value from memory into A
		"staout( 2=ra,aw & 3=rb,wm & 4=ei", // Use reg A as memory address, then copy value from B into memory
		"ldlge( 2=bnk,ir & 3=cr,aw & 4=ce,rm,aw & 5=rm,wa & 6=ei", // Use value directly after counter as address, then copy value from memory to reg A and advance counter by 2
		"stlge( 2=bnk,ir & 3=cr,aw & 4=ce,rm,aw & 5=ra,wm & 6=ei", // Use value directly after counter as address, then copy value from reg A to memory and advance counter by 2
		"ldw( 2=bnk,ir & 3=cr,aw & 4=ce,rm,wa & 5=ei", // Load value directly after counter into A, and advance counter by 2
		"swp( 2=ra,wc & 3=wa,rb & 4=rc,wb & 5=ei", // Swap register A and register B (this will overwrite the contents of register C, using it as a temporary swap area)
		"swpc( 2=ra,wb & 3=wa,rc & 4=rb,wc & 5=ei", // Swap register A and register C (this will overwrite the contents of register B, using it as a temporary swap area)
		"pcr( 2=cr,wa & 3=ei", // Program counter read, get the current program counter value and put it into register A
		"bsl( 2=sl,wa,eo,fl & 3=ei", // Bit shift left A register, the number of bits to shift determined by the value in register B
		"bsr( 2=sr,wa,eo,fl & 3=ei", // Bit shift right A register, the number of bits to shift determined by the value in register B
		"and( 2=and,wa,eo,fl & 3=ei", // Logical AND operation on register A and register B, with result put back into register A
		"or( 2=or,wa,eo,fl & 3=ei", // Logical OR operation on register A and register B, with result put back into register A
		"not( 2=not,wa,eo,fl & 3=ei", // Logical NOT operation on register A, with result put back into register A
		"bnk( 2=bnk,ir & 3=ei", // Change bank, changes the memory bank register to the value specified <val>
		"vbuf( 2=vbf & 3=ei", // Swap the video buffer
		"bnkc( 2=rc,bnk & 3=ei", // Change bank to C register
		"ldwb( 2=bnk,ir & 3=cr,aw & 4=ce,rm,wb & 5=ei", // Load value directly after counter into B, and advance counter by 2
	};

    private static IReadOnlyList<Instruction>? _default;

    public static IReadOnlyList<Instruction> Default
    {
        get => _default ??= Parse(DefaultInstructions);
        set => _default = value;
    }
}
