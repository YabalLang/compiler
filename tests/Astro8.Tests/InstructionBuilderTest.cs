using Astro8.Devices;
using Astro8.Instructions;
using Moq;
using Xunit.Abstractions;

namespace Astro8.Tests;

public class InstructionBuilderTest
{
    private readonly ITestOutputHelper _output;

    public InstructionBuilderTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private Cpu<Handler> Create(InstructionBuilder builder)
    {
        _output.WriteLine(builder.ToString());

        var mock = Mock.Of<Handler>();
        var cpu = CpuBuilder.Create(mock)
            .WithMemory()
            .WithProgram(builder)
            .Create();

        return cpu;
    }

    [Theory]
    [InlineData("ADD", 4, 2)]
    [InlineData("SUB", 0, 2)]
    [InlineData("MULT", 4, 2)]
    [InlineData("DIV", 1, 2)]
    public void Instruction_Calculations(string instruction, int a, int b)
    {
        var builder = new Instructions.InstructionBuilder()
            .SetA(2)
            .SetB(2)
            .Emit(instruction);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((a, b, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_Jump()
    {
        var builder = new Instructions.InstructionBuilder()
            .CreateLabel(out var label)
            .Jump(label)
            .SetA(10)
            .Mark(label)
            .SetB(10);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((0, 10, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_LDLGE()
    {
        var builder = new Instructions.InstructionBuilder()
            .CreateLabel("end", out var end)
            .CreateLabel("data", out var data)
            .LoadA_Large(data)
            .Jump(end)
            .Mark(data)
            .EmitRaw(int.MaxValue)
            .Mark(end);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((int.MaxValue, 0, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_JREG()
    {
        var builder = new Instructions.InstructionBuilder()
            .CreateLabel(out var label)
            .SetA(label)
            .JumpToA()
            .SetB(10)
            .Mark(label);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((label.Value, 0, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void InstructionBuilder_ReusePointer()
    {
        new Instructions.InstructionBuilder()
            .EmitRaw(0)
            .CreatePointer(out var pointerA)
            .CreatePointer(out var pointerB)
            .ToArray();

        Assert.Equal(pointerA, pointerB);
        Assert.Equal(0, pointerA.Value);
    }

    [Fact]
    public void InstructionBuilder_NewPointer()
    {
        new Instructions.InstructionBuilder()
            .EmitRaw(0)
            .CreatePointer(out var pointerA)
            .EmitRaw(0)
            .CreatePointer(out var pointerB)
            .ToArray();

        Assert.NotEqual(pointerA, pointerB);
        Assert.Equal(0, pointerA.Value);
        Assert.Equal(1, pointerB.Value);
    }
}
