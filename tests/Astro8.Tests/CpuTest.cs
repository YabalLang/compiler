using Xunit.Abstractions;

namespace Astro8.Tests;

public class CpuTest
{
    private ITestOutputHelper _output;

    public CpuTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    // A
    [InlineData("a", "b", 2, 2, 3)]
    [InlineData("a", "c", 3, 2, 3)]
    [InlineData("a", "4", 4, 2, 3)]
    [InlineData("a", "4000", 4000, 2, 3)]

    // B
    [InlineData("b", "a", 1, 1, 3)]
    [InlineData("b", "c", 1, 3, 3)]
    [InlineData("b", "4", 1, 4, 3)]
    [InlineData("b", "4000", 1, 4000, 3)]

    // C
    [InlineData("c", "a", 1, 2, 1)]
    [InlineData("c", "b", 1, 2, 2)]

    public void TestMovAB(string target, string value, int a, int b, int c)
    {
        var builder = new Assembler();
        builder.Mov(target, value);
        var bytes = builder.ToArray();

        var memory = new Memory(bytes);
        var cpu = new Cpu(memory)
        {
            A = 1,
            B = 2,
            C = 3
        };

        cpu.Run();

        Assert.Equal((a, b, c), (cpu.A, cpu.B, cpu.C));
    }

    [Theory]
    [InlineData(InstructionReference.ADD, 4, 2)]
    [InlineData(InstructionReference.SUB, 0, 2)]
    [InlineData(InstructionReference.MULT, 4, 2)]
    [InlineData(InstructionReference.DIV, 1, 2)]
    public void Instruction_Calculations(int instruction, int a, int b)
    {
        var instructions = new InstructionBuilder()
            .SetA(2)
            .SetB(2)
            .Emit(instruction)
            .ToArray();

        var memory = new Memory(instructions);
        var cpu = new Cpu(memory);

        cpu.Run();

        Assert.Equal((a, b, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_Jump()
    {
        var instructions = new InstructionBuilder()
            .CreateLabel(out var label)
            .Jump(label)
            .SetA(10)
            .Mark(label)
            .SetB(10)
            .ToArray();

        var memory = new Memory(instructions);
        var cpu = new Cpu(memory);

        cpu.Run();

        Assert.Equal((0, 10, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_LDLGE()
    {
        var builder = new InstructionBuilder()
            .CreateLabel("end", out var end)
            .CreateLabel("data", out var data)
            .LoadA_Large(data)
            .Jump(end)
            .Mark(data)
            .EmitRaw(int.MaxValue)
            .Mark(end);

        _output.WriteLine(builder.ToString());

        var memory = new Memory(builder.ToArray());
        var cpu = new Cpu(memory);

        cpu.Run();

        Assert.Equal((int.MaxValue, 0, 0), (cpu.A, cpu.B, cpu.C));
    }
}
