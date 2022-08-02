using Astro8.Devices;
using Astro8.Instructions;
using Moq;
using Xunit.Abstractions;

namespace Astro8.Tests;

public class AssemblerTest
{
    private readonly ITestOutputHelper _output;

    public AssemblerTest(ITestOutputHelper output)
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

    public void Instruction_Mov(string target, string value, int a, int b, int c)
    {
        var builder = new Assembler();
        builder.Mov(target, value);

        var cpu = Create(builder.InstructionBuilder);

        cpu.A = 1;
        cpu.B = 2;
        cpu.C = 3;
        cpu.Run();

        Assert.Equal((a, b, c), (cpu.A, cpu.B, cpu.C));
    }
}
