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

    private Cpu<Handler> Create(YabalBuilder builder)
    {
        _output.WriteLine("Instructions:");
        _output.WriteLine(builder.Instruction.ToString());

        var mock = Mock.Of<Handler>();
        var cpu = CpuBuilder.Create(mock)
            .WithMemory()
            .WithProgram(builder.Instruction)
            .Create();

        return cpu;
    }

    [Fact]
    public void Push()
    {
        var builder = new YabalBuilder();

        var value = builder.Instruction.Nop().CreatePointer("Counter");
        var function = builder.Instruction.CreateLabel("Function");

        // Functions
        builder.Instruction.Mark(function);
        builder.Instruction.LoadA(value);
        builder.Instruction.SetB(1);
        builder.Instruction.Add();
        builder.Instruction.StoreA(value);
        builder.Ret();

        // Program
        builder.MarkProgram();

        builder.Instruction.SetA(1);
        builder.Instruction.StoreA(builder.Stack[0]);

        builder.Call(function);
        builder.Call(function);

        // Run
        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(2, cpu.Memory[value.Address]);
        Assert.Equal(1, cpu.Memory[builder.Stack[0].Address]);
    }

    [Theory]
    [InlineData("+", 4)]
    [InlineData("-", 0)]
    [InlineData("*", 4)]
    [InlineData("/", 1)]
    public void Binary(string type, int expected)
    {
        var code = $"""
            var a = 2;
            var b = 2

            a = a {type} b;
            """;

        var builder = new YabalBuilder();
        builder.CompileCode(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(expected, cpu.Memory[address]);
    }

    [Theory]
    [InlineData("+=", 4)]
    [InlineData("-=", 0)]
    [InlineData("*=", 4)]
    [InlineData("/=", 1)]
    public void BinaryEqual(string type, int expected)
    {
        var code = $"""
            var a = 2;

            a {type} 2;
            """;

        var builder = new YabalBuilder();
        builder.CompileCode(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(expected, cpu.Memory[address]);
    }
}
