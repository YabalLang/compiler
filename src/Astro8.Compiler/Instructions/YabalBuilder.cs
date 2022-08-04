using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Astro8.Yabal;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class YabalBuilder
{
    private const int STACK_SIZE = 16;

    private readonly Dictionary<int, InstructionPointer> _values = new();
    private readonly InstructionPointer _stackIndex;
    private int _valueOffset;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly InstructionLabel _programLabel;
    private readonly Stack<BlockStack> _blockStack = new();
    private readonly InstructionPointer[] _stack = new InstructionPointer[STACK_SIZE - 1];

    public YabalBuilder()
    {
        Instruction = new InstructionBuilder();
        _valueOffset = Instruction.Count;

        _callLabel = Instruction.CreateLabel("Call");
        _returnLabel = Instruction.CreateLabel("Return");
        _programLabel = Instruction.CreateLabel("Program");

        Instruction.Jump(_programLabel);
        Temp = Instruction.EmitRaw(0).CreatePointer(name: "Temp");
        _stackIndex = Instruction.EmitRaw(0xE000).CreatePointer(name: "CallPointer");

        for (var i = 0; i < _stack.Length; i++)
        {
            _stack[i] = Instruction.EmitRaw(0).CreatePointer(name: $"Stack:{i}");
        }

        CreateCall();
        CreateReturn();

        Instruction.Mark(_programLabel);
        PushBlock();
    }

    public IReadOnlyList<InstructionPointer> Stack => _stack;

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

    public void MarkProgram() => Instruction.Mark(_programLabel);

    private void CreateCall()
    {
        // B = Return address
        // C = Method address

        Instruction.Mark(_callLabel);

        // Store return address
        Instruction.LoadA(_stackIndex);
        Instruction.StoreB_ToAddressInA();

        // Store values on stack
        for (var i = 0; i < _stack.Length; i++)
        {
            Instruction.SetB(1);
            Instruction.Add();

            Instruction.LoadB(_stack[i]);
            Instruction.StoreB_ToAddressInA();
        }

        // Increment stack pointer
        Instruction.LoadA(_stackIndex);
        Instruction.SetB(STACK_SIZE);
        Instruction.Add();
        Instruction.StoreA(_stackIndex);

        // Go to the address
        Instruction.SwapA_C();
        Instruction.JumpToA();
    }

    private void CreateReturn()
    {
        Instruction.Mark(_returnLabel);

        // Decrement the stack pointer
        Instruction.LoadA(_stackIndex);
        Instruction.SetB(STACK_SIZE);
        Instruction.Sub();
        Instruction.StoreA(_stackIndex);

        // Restore values from the stack
        Instruction.SetB(1);

        for (var i = 0; i < _stack.Length; i++)
        {
            Instruction.Add();
            Instruction.LoadA_FromAddressUsingA();
            Instruction.StoreA(_stack[i]);
        }

        // Go to the return address
        Instruction.LoadA(_stackIndex);
        Instruction.LoadA_FromAddressUsingA();
        Instruction.JumpToA();
    }

    public InstructionBuilder Instruction { get; }

    public void Call(PointerOrData address)
    {
        var returnAddress = Instruction.CreateLabel();

        Instruction.SetA(address);
        Instruction.SwapA_C();
        Instruction.SetB(returnAddress);
        Instruction.Jump(_callLabel);

        Instruction.Mark(returnAddress);
    }

    public void Ret()
    {
        Instruction.Jump(_returnLabel);
    }

    public InstructionPointer CreateValuePointer(int value)
    {
        if (_values.TryGetValue(value, out var pointer))
        {
            return pointer;
        }

        pointer = Instruction.EmitRawAt(_valueOffset++, value);
        _values[value] = pointer;
        return pointer;
    }

    private void LoadA(int address)
    {
        if (address < InstructionReference.MaxDataLength)
        {
            Instruction.LoadA(address);
        }
        else
        {
            Instruction.LoadA_Large(address);
        }
    }
}
