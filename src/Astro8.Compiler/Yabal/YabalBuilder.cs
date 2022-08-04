using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Astro8.Yabal;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class YabalBuilder : InstructionBuilderBase, IProgram
{
    private readonly InstructionBuilder _builder;

    private readonly Dictionary<int, InstructionPointer> _values = new();
    private readonly InstructionPointer _stackIndex;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly Stack<BlockStack> _blockStack = new();
    private readonly List<InstructionPointer> _stack = new();
    private bool _hasCall;

    public YabalBuilder()
    {
        _builder = new InstructionBuilder();

        _callLabel = _builder.CreateLabel("Call");
        _returnLabel = _builder.CreateLabel("Return");

        Temp = _builder.CreatePointer(name: "Temp");

        _stackIndex = new InstructionPointer(name: "CallPointer");

        PushBlock();
    }

    public InstructionPointer GetLargeValue(int value)
    {
        if (_values.TryGetValue(value, out var pointer))
        {
            return pointer;
        }

        pointer = new InstructionPointer($"Constant:{value}");
        _values[value] = pointer;
        return pointer;
    }

    public InstructionPointer GetStackVariable(int index)
    {
        if (index < _stack.Count)
        {
            return _stack[index];
        }

        var pointer = new InstructionPointer($"Stack:{index}");
        _stack.Add(pointer);
        return pointer;
    }

    public InstructionPointer Temp { get; set; }

    public BlockStack Block => _blockStack.Peek();

    public void PushBlock()
    {
        _blockStack.Push(new BlockStack());
    }

    public void PopBlock()
    {
        _blockStack.Pop();
    }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
    {
        foreach (var block in _blockStack)
        {
            if (block.Variables.TryGetValue(name, out variable))
            {
                return true;
            }
        }

        variable = default;
        return false;
    }

    public Variable GetVariable(string name)
    {
        if (!TryGetVariable(name, out var variable))
        {
            throw new KeyNotFoundException($"Variable '{name}' not found");
        }

        return variable;
    }

    public void CompileCode(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new YabalLexer(inputStream);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new YabalParser(commonTokenStream)
        {
            ErrorHandler = new BailErrorStrategy(),
        };

        var listener = new YabalVisitor();
        var program = listener.VisitProgram(parser.program());

        program.Build(this);
    }

    private void CreateCall(InstructionBuilderBase builder)
    {
        // B = Return address
        // C = Method address

        builder.Mark(_callLabel);

        // Store return address
        builder.LoadA(_stackIndex);
        builder.StoreB_ToAddressInA();

        // Store values on stack
        for (var i = 0; i < _stack.Count; i++)
        {
            builder.SetB(1);
            builder.Add();

            builder.LoadB(_stack[i]);
            builder.StoreB_ToAddressInA();
        }

        // Increment stack pointer
        builder.LoadA(_stackIndex);
        builder.SetB(_stack.Count + 1);
        builder.Add();
        builder.StoreA(_stackIndex);

        // Go to the address
        builder.SwapA_C();
        builder.JumpToA();
    }

    private void CreateReturn(InstructionBuilderBase builder)
    {
        builder.Mark(_returnLabel);

        // Decrement the stack pointer
        builder.LoadA(_stackIndex);
        builder.SetB(_stack.Count + 1);
        builder.Sub();
        builder.StoreA(_stackIndex);

        // Restore values from the stack
        builder.SetB(1);

        for (var i = 0; i < _stack.Count; i++)
        {
            builder.Add();
            builder.LoadA_FromAddressUsingA();
            builder.StoreA(_stack[i]);
        }

        // Go to the return address
        builder.LoadA(_stackIndex);
        builder.LoadA_FromAddressUsingA();
        builder.JumpToA();
    }

    public void Call(PointerOrData address)
    {
        _hasCall = true;

        var returnAddress = _builder.CreateLabel();

        _builder.SetA(address);
        _builder.SwapA_C();
        _builder.SetB(returnAddress);
        _builder.Jump(_callLabel);

        _builder.Mark(returnAddress);
    }

    public void Ret()
    {
        _hasCall = true;

        _builder.Jump(_returnLabel);
    }

    public int? Index
    {
        get => _builder.Index;
        set => _builder.Index = value;
    }

    public override int Count => _builder.Count;

    public override InstructionBuilder.RegisterWatch WatchRegister()
    {
        return _builder.WatchRegister();
    }

    public override InstructionLabel CreateLabel(string? name = null)
    {
        return _builder.CreateLabel(name);
    }

    public override InstructionPointer CreatePointer(string? name = null, int? index = null)
    {
        return _builder.CreatePointer(name, index);
    }

    public override void Mark(InstructionPointer pointer)
    {
        _builder.Mark(pointer);
    }

    public override void Emit(string name, PointerOrData either = default, int? index = null)
    {
        _builder.Emit(name, either, index);
    }

    public override void EmitRaw(PointerOrData value)
    {
        _builder.EmitRaw(value);
    }

    public void CopyTo(int[] array, int offset)
    {
        var builder = CreateFinalBuilder();

        builder.CopyTo(array, offset);
    }

    public override string ToString()
    {
        return CreateFinalBuilder().ToString();
    }

    private InstructionBuilder CreateFinalBuilder()
    {
        var builder = new InstructionBuilder();
        var programLabel = builder.CreateLabel("Program");

        builder.Jump(programLabel);

        builder.Mark(Temp);
        builder.EmitRaw(0);

        if (_hasCall)
        {
            builder.Mark(_stackIndex);
            builder.EmitRaw(0xE000);

            foreach (var pointer in _stack)
            {
                builder.Mark(pointer);
                builder.EmitRaw(0);
            }

            CreateCall(builder);
            CreateReturn(builder);
        }

        builder.Mark(programLabel);

        builder.AddRange(_builder);
        return builder;
    }
}
