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
        if (builder.Errors.Count > 0)
        {
            _output.WriteLine("Errors:");
            foreach (var error in builder.Errors.SelectMany(i => i.Value))
            {
                _output.WriteLine(error.Message);
            }

            _output.WriteLine("");
        }

        _output.WriteLine("Instructions:");
        _output.WriteLine(builder.Build().ToAssembly(addComments: true));

        var mock = Mock.Of<Handler>();
        var cpu = CpuBuilder.Create(mock)
            .WithMemory(0xFFFF)
            .WithProgram(builder)
            .Create();

        return cpu;
    }

    [Fact]
    public async Task Push()
    {
        var builder = new YabalBuilder();

        builder.Nop();
        var value = builder.CreatePointer("Counter");
        var function = builder.CreateLabel("Function");
        var skipFunction = builder.CreateLabel("Main");
        var stackVariable = builder.Stack.Get(0, 1);

        builder.Jump(skipFunction);

        // Functions
        builder.Mark(function);
        builder.LoadA(value);
        builder.SetB(1);
        builder.Add();
        builder.StoreA(value);
        builder.Ret();

        // Program
        builder.Mark(skipFunction);

        builder.SetA(1);
        builder.StoreA(stackVariable);

        builder.Call(function);
        builder.Call(function);

        // Run
        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(2, cpu.Memory[value.Address]);
        Assert.Equal(1, cpu.Memory[stackVariable.Address]);
    }

    [Theory]
    [InlineData("+", 2, 2, 4)]
    [InlineData("-", 2, 2, 0)]
    [InlineData("*", 2, 2, 4)]
    [InlineData("/", 6, 2, 3)]
    [InlineData("%", 5, 2, 1)]
    [InlineData("&", 0b10, 0b11, 0b10)]
    [InlineData("|", 0b10, 0b11, 0b11)]
    [InlineData("<<", 0b1, 1, 0b10)]
    [InlineData(">>", 0b10, 1, 0b1)]
    public async Task Binary(string type, int left, int right, int expected)
    {
        var code = $$"""
            var leftConstant = {{ left }}
            var rightConstant = {{ right }}
            int left; left = {{ left }}
            int right; right = {{ right }}
            var optimized = {{ left }} {{ type }} {{ right }}
            var constant = leftConstant {{ type }} rightConstant
            var fast = left {{ type }} {{ right }}
            var slow = left {{ type }} right
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("optimized").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("constant").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("fast").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("slow").Pointer.Address]);
    }

    [Theory]
    [InlineData("+=", 4)]
    [InlineData("-=", 0)]
    [InlineData("*=", 4)]
    [InlineData("/=", 1)]
    public async Task BinaryEqual(string type, int expected)
    {
        var code = $"""
            var a = 2;

            a {type} 2;
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(expected, cpu.Memory[address]);
    }

    [Fact]
    public async Task Function()
    {
        const string code = """
            var a = 0

            void functionA(int amount) {
                var currentAmount = a
                var alsoIncreaseWith = functionB()

                a = currentAmount + amount + alsoIncreaseWith
            }

            int functionB() {
                var value = 1

                return value
            }

            functionA(2)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(3, cpu.Memory[address]);
    }

    [Fact]
    public async Task FunctionArgument()
    {
        const string code = """
            var called = 0

            int get(int value) {
                called = 1
                return value
            }

            var value = get(1)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("called").Pointer.Address]);
        Assert.Equal(1, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
    }

    [Fact]
    public async Task FunctionReturn()
    {
        const string code = """
            int get_offset(int x, int y) {
                return x * 64 + y
            }

            int get_color(int r, int g, int b) {
                return (r / 8 << 10) + (g / 8 << 5) + (b / 8)
            }

            var screen = create_pointer(61439)

            for (var x = 1; x <= 16; x++) {
                for (var y = 1; y <= 16; y++) {
                    screen[get_offset(x, y)] = get_color(255, 0, 0)
                }
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        for (var x = 1; x <= 16; x++)
        {
            for (var y = 1; y <= 16; y++)
            {
                var offset = x * 64 + y;
                var address = 61439 + offset;

                Assert.Equal(0b111110000000000, cpu.Memory[address]);
            }
        }
    }

    [Fact]
    public async Task InlineAsm()
    {
        const string code = """
            var result = 0

            void increment_loop(int amount) {
                asm {
                    _increment:
                    STA 1
                    BIN @result
                    ADD
                    STA @result
                    BIN @amount
                    JL _increment
                }
            }

            increment_loop(10)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("result").Pointer.Address;
        Assert.Equal(10, cpu.Memory[address]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pointer(bool @const)
    {
        var code = $$"""
            var index = 1
            var value = 2
            int[] memory{{(@const ? " " : "; memory ")}}= create_pointer(4095)

            memory[index] = value
            value = memory[index]
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        if (!@const)
        {
            Assert.Equal(4095, cpu.Memory[builder.GetVariable("memory").Pointer.Address]);
        }

        Assert.Equal(2, cpu.Memory[4096]);
        Assert.Equal(2, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
    }

    [Fact]
    public async Task PointerToBank()
    {
        const string code = $$"""
            var memory = create_pointer(4095, 1)

            void set_inner(int[] memory, int index, int value) {
                memory[index] = value
            }

            void set(int[] memory, int index, int value) {
                set_inner(memory, index, value)
            }

            set(memory, 0, 1)
            """ ;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Banks[1][4095]);
    }

    [Theory]
    [InlineData(">", 10, -1, 0, 0)]
    [InlineData("<", 10, -1, 0, 10)]
    [InlineData(">=", 10, -1, 0, -1)]
    [InlineData("<=", 10, -1, 0, 10)]

    [InlineData(">", 0, 1, 10, 0)]
    [InlineData("<", 0, 1, 10, 10)]
    [InlineData(">=", 0, 1, 10, 0)]
    [InlineData("<=", 0, 1, 10, 11)]

    [InlineData("==", 10, -1, 10, 9)]
    [InlineData("!=", 10, -1, 0, 0)]
    public async Task While(string type, int start, int increment, int end, int expected)
    {
        var code = $$"""
            var value = {{ start }}

            while (value {{ type }} {{ end }}) {
                value += {{ increment }}
            }
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
    }

    [Fact]
    public async Task WhileVariable()
    {
        const string code = $$"""
            var run = true
            var value = 10;

            while (run) {
                value -= 1
                run = value > 0
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(0, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
    }

    [Theory]
    [InlineData(10, ">", 0, 1)]
    [InlineData(9, ">", 0, 1)]
    [InlineData(10, "<", 0, 0)]
    [InlineData(0, "<", 0, 0)]
    [InlineData("true", "&&", "true", 1)]
    [InlineData("true", "&&", "false", 0)]
    [InlineData("false", "&&", "false", 0)]
    [InlineData("false", "&&", "true", 0)]
    [InlineData("true", "||", "true", 1)]
    [InlineData("true", "||", "false", 1)]
    [InlineData("false", "||", "false", 0)]
    [InlineData("false", "||", "true", 1)]
    public async Task Compare(object left, string type, object right, int expected)
    {
        var code = $$"""
            const var leftConstant = {{ left }}
            const var rightConstant = {{ right }}
            var left = {{ left }}
            var right = {{ right }}
            var optimized = {{ left }} {{ type }} {{ right }}
            var constant = leftConstant {{ type }} rightConstant
            var fast = left {{ type }} {{ right }}
            var slow = left {{ type }} right
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("optimized").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("constant").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("fast").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("slow").Pointer.Address]);
    }

    [Fact]
    public async Task For()
    {
        const string code = $$"""
            var value = 0

            for (var i = 0; i < 10; i++) {
                value += 1
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(10, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task If(int step, bool optimize)
    {
        var code = $$"""
            var result = 0
            var value = {{step}}

            if (value == 1) {
                result = 1
            } else if (value == 2) {
                result = 2
            } else {
                result = 3
            }
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(step, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task Switch(int step, bool optimized)
    {
        var code = $$"""
            var result = 0
            var value = {{(optimized ? "" : "0; value = ") + step}}

            var result = value switch {
                1 => 1,
                2 => 2,
                _ => 3
            }
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(step, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData("true", 0, false)]
    [InlineData("true", 0, true)]
    [InlineData("false", 1, false)]
    [InlineData("false", 1, true)]
    public async Task Negate(string input, int expected, bool optimized)
    {
        var code = $"""
            bool value{(optimized ? $" = {input}" : $"; value = {input}")}
            var result = !value
            """;

        _output.WriteLine("Code:");
        _output.WriteLine(code);
        _output.WriteLine("");

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StructToArray(bool @const)
    {
        var code = $$"""
            struct Test {
                int a
                int b
            }

            {{(@const ? "const " : "")}}var pointer = create_pointer<Test>(4095)

            Test first
            first.a = 1
            first.b = 2
            pointer[0] = first

            Test second
            second.a = 3
            second.b = 4
            pointer[1] = second
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[4095]);
        Assert.Equal(2, cpu.Memory[4096]);

        Assert.Equal(3, cpu.Memory[4097]);
        Assert.Equal(4, cpu.Memory[4098]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PointerToStruct(bool optimize)
    {
        const string code = $$"""
            struct Test {
                int a
                int b
            }

            var pointer = create_pointer<Test>(4095)

            var first = pointer[0]
            var a = first.a
            var b = first.b

            int index = 1
            var second = pointer[index]
            var c = second.a
            var d = second.b
            """ ;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Memory[4095] = 1;
        cpu.Memory[4096] = 2;
        cpu.Memory[4097] = 3;
        cpu.Memory[4098] = 4;
        cpu.Run();

        Assert.Equal(4095, cpu.Memory[builder.GetVariable("pointer").Pointer.Address]);
        Assert.Equal(1, cpu.Memory[builder.GetVariable("a").Pointer.Address]);
        Assert.Equal(2, cpu.Memory[builder.GetVariable("b").Pointer.Address]);
        Assert.Equal(3, cpu.Memory[builder.GetVariable("c").Pointer.Address]);
        Assert.Equal(4, cpu.Memory[builder.GetVariable("d").Pointer.Address]);
    }

    [Fact]
    public async Task Struct()
    {
        const string code = $$"""
            struct Test {
                int a
                int b
            }

            Test test
            test.a = 1
            test.b = 2
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("test").Pointer.Address;
        Assert.Equal(1, cpu.Memory[address]);
        Assert.Equal(2, cpu.Memory[address + 1]);
    }

    [Fact]
    public async Task StructDeep()
    {
        const string code = $$"""
            struct Position {
                int x
                int y
            }

            struct ScreenColor {
                int color
                int alpha
                Position pos
            }

            ScreenColor color
            color.color = 1
            color.alpha = 2
            color.pos.x = 3
            color.pos.y = 4
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("color").Pointer.Address;
        Assert.Equal(1, cpu.Memory[address]);
        Assert.Equal(2, cpu.Memory[address + 1]);
        Assert.Equal(3, cpu.Memory[address + 2]);
        Assert.Equal(4, cpu.Memory[address + 3]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructBit(bool @const)
    {
        var code = $$"""
            int x
            x = 1

            MouseInput input
            input.y = 2
            input.x = {{(@const ? "1" : "x")}}

            struct MouseInput {
                int y : 7;
                int x : 7;
                int left : 1;
                int right : 1;
            };
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        const int expected = 0b0_0_0000001_0000010;

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("input").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructBitInitializer(bool @const)
    {
        var code = $$"""
            int x
            x = 1

            MouseInput input = { y: 2, x: {{(@const ? "1" : "x")}} }

            struct MouseInput {
                int y : 7;
                int x : 7;
                int left : 1;
                int right : 1;
            };
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code);

        var cpu = Create(builder);
        cpu.Run();

        const int expected = 0b0_0_0000001_0000010;

        Assert.Equal(expected, cpu.Memory[builder.GetVariable("input").Pointer.Address]);
    }
}
