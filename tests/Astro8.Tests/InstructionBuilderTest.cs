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
        _output.WriteLine("Instructions:");
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
    public void Binary(string instruction, int a, int b)
    {
        var builder = new InstructionBuilder();
        builder.SetA(2);
            builder.SetB(2);
            builder.Emit(instruction);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((a, b, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void JMP()
    {
        var builder = new InstructionBuilder();
        builder.CreateLabel(out var label);
        builder.Jump(label);
        builder.SetA(10);
        builder.Mark(label);
        builder.SetB(10);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((0, 10, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void LDLGE()
    {
        var builder = new InstructionBuilder();
        builder.CreateLabel("END", out var end);
        builder.CreatePointer(out var data);
        builder.LoadA_Large(data);
        builder.Jump(end);
        builder.Mark(data);
        builder.EmitRaw(int.MaxValue);
        builder.Mark(end);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((int.MaxValue, 0, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void JREG()
    {
        var builder = new InstructionBuilder();
        builder.CreateLabel(out var label);
        builder.SetA(label);
        builder.JumpToA();
        builder.SetB(10);
        builder.Mark(label);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal((label.Address, 0, 0), (cpu.A, cpu.B, cpu.C));
    }

    [Fact]
    public void Pointer()
    {
        var builder = new InstructionBuilder();
        builder.EmitRaw(0);
        builder.CreatePointer(out var pointerA);
        builder.EmitRaw(0);
        builder.CreatePointer(out var pointerB);
        builder.ToArray();

        Assert.NotEqual(pointerA, pointerB);
        Assert.Equal(0, pointerA.Address);
        Assert.Equal(1, pointerB.Address);
    }
}
