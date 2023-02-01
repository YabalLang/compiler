using System.IO.Abstractions.TestingHelpers;
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
    public void Push()
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
    [InlineData("^", 0b10, 0b11, 0b01)]
    [InlineData("^", 0b10, 0b10, 0b00)]
    [InlineData("^", 0b11, 0b11, 0b00)]
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
    [InlineData("+=", 4, false)]
    [InlineData("+=", 4, true)]
    [InlineData("-=", 0, false)]
    [InlineData("-=", 0, true)]
    [InlineData("*=", 4, false)]
    [InlineData("*=", 4, true)]
    [InlineData("/=", 1, false)]
    [InlineData("/=", 1, true)]
    public async Task BinaryEqual(string type, int expected, bool optimize)
    {
        var code = $"""
            var a = 2;

            a {type} 2;
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(expected, cpu.Memory[address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Function(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("a").Pointer.Address;
        Assert.Equal(3, cpu.Memory[address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FunctionArgument(bool optimize)
    {
        const string code = """
            var result = 0

            int get(int value) {
                result = value
                return value
            }

            var returnValue = get(1)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
        Assert.Equal(1, cpu.Memory[builder.GetVariable("returnValue").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FunctionInline(bool optimize)
    {
        const string code = """
            inline int get_offset(int x, int y) {
                return x * 64 + y
            }

            inline int get_color(int r, int g, int b) {
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
        await builder.CompileCodeAsync(code, optimize);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InlineAsm(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PointerToBank(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhileVariable(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task For(bool optimize)
    {
        const string code = $$"""
            var value = 0

            for (var i = 0; i < 10; i++) {
                value += 1
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

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
    public async Task Switch(int step, bool optimize)
    {
        var code = $$"""
            var result = 0
            var value = {{step}}

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
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(step, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData("true", 0, false)]
    [InlineData("true", 0, true)]
    [InlineData("false", 1, false)]
    [InlineData("false", 1, true)]
    public async Task Negate(string input, int expected, bool optimize)
    {
        var code = $"""
            bool value{(optimize ? $" = {input}" : $"; value = {input}")}
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Struct(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("test").Pointer.Address;
        Assert.Equal(1, cpu.Memory[address]);
        Assert.Equal(2, cpu.Memory[address + 1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StructDeep(bool optimize)
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
        await builder.CompileCodeAsync(code, optimize);

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ForContinue(bool optimize)
    {
        const string code = """
            var result = 0

            for (var i = 0; i < 3; i++) {
                if (i == 1) {
                    continue
                }

                result += i
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(2, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FunctionReturn(bool inline, bool optimize)
    {
        var code = $$"""
            {{(inline ? "inline " : "")}}int return_value(bool returnOne) {
                if (returnOne) {
                    return 1
                }

                return 2
            }

            var a = return_value(true)
            var b = return_value(false)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("a").Pointer.Address]);
        Assert.Equal(2, cpu.Memory[builder.GetVariable("b").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WriteChars(bool optimize)
    {
        const string code = """
            var chars = create_pointer(0xD12A, 1)
            var message = "TEST"
            var size = sizeof(message)

            for (int i = 0; i < sizeof(message); i++) {
                chars[i] = message[i]
            }
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var offset = 0xD12A;
        var bank = cpu.Banks[1];
        var str = builder.GetString("TEST");

        Assert.Equal(offset, cpu.Memory[builder.GetVariable("chars").Pointer.Address]);
        Assert.Equal(str.Address, cpu.Memory[builder.GetVariable("message").Pointer.Address]);

        Assert.Equal(4, cpu.Memory[builder.GetVariable("size").Pointer.Address]);
        Assert.Equal(Character.CharToInt['T'], bank[offset]);
        Assert.Equal(Character.CharToInt['E'], bank[offset + 1]);
        Assert.Equal(Character.CharToInt['S'], bank[offset + 2]);
        Assert.Equal(Character.CharToInt['T'], bank[offset + 3]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadFromBank(bool optimize)
    {
        const string code = """
            var exps = create_pointer(53500, 1)
            var result = exps[0]
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        var bank = cpu.Banks[1];
        bank[53500] = 1;

        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyBank(bool optimize)
    {
        const string code = """
            var pixels = create_pointer(53871, 1)
            var exps = create_pointer(53500, 1)

            pixels[54] = exps[0] << 4
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        var bank = cpu.Banks[1];
        bank[53500] = 1;

        cpu.Run();

        Assert.Equal(1 << 4, bank[53871 + 54]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructBitReturn(bool optimize)
    {
        const string code = """
            var result = 0;

            struct Float16 {
                int sign : 1
                int exponent : 8
                int fraction : 7
            }

            void set_result(int value) {
                result = value
            }

            Float16 f;
            f.sign = 1;
            f.exponent = 5;

            set_result(f.exponent)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(5, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Not(bool optimize)
    {
        const string code = """
            var pointer = create_pointer(4095)
            int prenotted = 0b10001111
            pointer[0] = ~prenotted
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(~0b10001111 & ushort.MaxValue, cpu.Memory[4095]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructBitLarge(bool optimize)
    {
        const string code = """
            struct SignedInt {
                int s : 1
                int val : 10
            };

            var pointer = create_pointer<SignedInt>(4095)

            pointer[0] = {
                s: 1,
                val: 1
            }

            pointer[1].s = 1
            pointer[1].val = 1

            SignedInt value
            value.s = 1
            value.val = 1
            pointer[2] = value
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        const int expected = 0b11;

        Assert.Equal(expected, cpu.Memory[4095]);
        Assert.Equal(expected, cpu.Memory[4096]);
        Assert.Equal(expected, cpu.Memory[builder.GetVariable("value").Pointer.Address]);
        Assert.Equal(expected, cpu.Memory[4097]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import(bool optimize)
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [@"lib.yabal"] = new("""
                var offset = 0;

                inline void set_offset(int value) {
                    offset = value
                }

                inline int get_offset() {
                    return offset
                }
                """)
        });

        const string code = """
            import "lib.yabal"

            var pointer = create_pointer(4095)

            set_offset(1)
            pointer[0] = get_offset()
            """;

        var builder = new YabalBuilder(fileSystem);
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[4095]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reference(bool optimize)
    {
        const string code = """
            void set_value(ref int value) {
                value = 2
            }

            var a = 1
            set_value(ref a)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(2, cpu.Memory[builder.GetVariable("a").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StackReference(bool optimize)
    {
        const string code = """
            void set_value(ref int value) {
                value = 2
            }

            int get_value() {
                var a = 1
                set_value(ref a)
                return a
            }

            int result = 0
            result = get_value()
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(2, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeepReference(bool optimize)
    {
        const string code = """
            void set_value(ref int value) {
                value = 1
            }

            void set_value_ref(ref int a) {
                set_value(a)
            }

            int result = 0
            set_value_ref(ref result)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeepReferenceInvalid(bool optimize)
    {
        const string code = """
            void set_value(ref int value) {
                value = 1
            }

            void set_value_ref(ref int a) {
                set_value(ref a)
            }

            int result = 0
            set_value_ref(ref result)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeepReferenceVariable(bool optimize)
    {
        const string code = """
            void set_value(ref int value) {
                value = 1
            }

            void set_value_ref(ref int a) {
                var b = a
                set_value(b)
            }

            int result = 0
            set_value_ref(ref result)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructReference(bool optimize)
    {
        const string code = """
            struct Test {
                int a
                int b
            }

            void set_value(ref Test value) {
                value.a = 1
                value.b = 2
            }

            Test test
            set_value(ref test)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("test").Pointer.Address;
        Assert.Equal(1, cpu.Memory[address]);
        Assert.Equal(2, cpu.Memory[address + 1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructReferenceBit(bool optimize)
    {
        const string code = """
            struct Test {
                int a : 4
                int b : 4
            }

            void set_value(ref Test value) {
                value.a = 1
                value.b = 1
            }

            Test test
            set_value(ref test)
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(17, cpu.Memory[builder.GetVariable("test").Pointer.Address]);
    }

    [Theory(Skip = "Not implemented yet")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StructReferenceStack(bool optimize)
    {
        const string code = """
            struct Test {
                int a
                int b
            }

            void set_value(ref Test value) {
                value.a = 1
                value.b = 2
            }

            Test get_value() {
                Test test
                set_value(ref test)
                return test
            }

            Test test
            test = get_value()
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        var address = builder.GetVariable("test").Pointer.Address;
        Assert.Equal(1, cpu.Memory[address]);
        Assert.Equal(2, cpu.Memory[address + 1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StackAlloc(bool optimize)
    {
        const string code = """
            int set_stackalloc() {
                var array = stackalloc int[2]
                array[0] = 1

                asm {
                    AIN @array
                    STLGE 4095
                }

                var array2 = stackalloc int[2]
                array2[1] = 1

                asm {
                    AIN @array2
                    STLGE 4096
                }

                return array[0] + array2[1]
            }

            var result = set_stackalloc()
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        // Ensure the stack allocation is increased
        Assert.Equal(0xEF6E, cpu.Memory[4095]);
        Assert.Equal(0xEF70, cpu.Memory[4096]);

        // Ensure the stack allocation is returned
        Assert.Equal(0xEF6E, cpu.Memory[builder.StackAllocPointer.Address]);

        // Check value
        Assert.Equal(1, cpu.Memory[0xEF6E]);
        Assert.Equal(2, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StackAllocPass(bool optimize)
    {
        const string code = """
            void set_value(int[] array) {
                array[0] = 1
            }

            int get_value() {
                var array = stackalloc int[2]
                set_value(array)
                return array[0]
            }

            var result = get_value()
            """;

        var builder = new YabalBuilder();
        await builder.CompileCodeAsync(code, optimize);

        var cpu = Create(builder);
        cpu.Run();

        Assert.Equal(1, cpu.Memory[builder.GetVariable("result").Pointer.Address]);
    }
}
