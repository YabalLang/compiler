using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Astro8.Yabal;
using Astro8.Yabal.Ast;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class YabalBuilder : InstructionBuilderBase, IProgram
{
    private readonly YabalBuilder? _parent;
    private readonly InstructionBuilder _builder;

    private readonly Dictionary<int, InstructionPointer> _values = new();
    private readonly InstructionPointer _stackIndex;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly Stack<BlockStack> _blockStack;
    private readonly List<InstructionPointer> _stack;
    private readonly Dictionary<string, Function> _functions = new();
    private bool _hasCall;

    public YabalBuilder()
    {
        _builder = new InstructionBuilder();

        _callLabel = _builder.CreateLabel("Call");
        _returnLabel = _builder.CreateLabel("Return");

        Temp = _builder.CreatePointer(name: "Temp");

        _stackIndex = new InstructionPointer(name: "CallPointer");
        _blockStack = new Stack<BlockStack>();
        _stack = new List<InstructionPointer>();

        PushBlock(true);
    }

    public YabalBuilder(YabalBuilder parent)
    {
        _parent = parent;
        _builder = new InstructionBuilder();

        _callLabel = parent._callLabel;
        _returnLabel = parent._returnLabel;

        Temp = parent.Temp;

        _stackIndex = parent._stackIndex;
        _blockStack = parent._blockStack;
        _stack = parent._stack;
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

    private void PushBlock(bool isGlobal)
    {
        _blockStack.Push(new BlockStack
        {
            IsGlobal = isGlobal
        });
    }

    public void PushBlock()
    {
        PushBlock(false);
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

        if (_parent != null)
        {
            return _parent.TryGetVariable(name, out variable);
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

    public bool TryGetFunction(string name, [NotNullWhen(true)] out Function? function)
    {
        if (_functions.TryGetValue(name, out function))
        {
            return true;
        }

        if (_parent != null)
        {
            return _parent.TryGetFunction(name, out function);
        }

        function = default;
        return false;
    }

    public Function GetFunction(string name)
    {
        if (!TryGetFunction(name, out var function))
        {
            throw new KeyNotFoundException($"Function '{name}' not found");
        }

        return function;
    }

    public void CompileCode(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new YabalLexer(inputStream);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new YabalParser(commonTokenStream)
        {
            // ErrorHandler = new BailErrorStrategy(),
        };

        var listener = new YabalVisitor();
        var program = listener.VisitProgram(parser.program());

        program.BeforeBuild(this);
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

    public LanguageType[] Call(PointerOrData address, IReadOnlyList<Expression>? arguments = null)
    {
        _hasCall = true;

        var hasArguments = arguments is { Count: > 0 };
        var argumentTypes = hasArguments ? new LanguageType[arguments!.Count] : Array.Empty<LanguageType>();
        var returnAddress = _builder.CreateLabel();
        var callAddress = _builder.CreateLabel();

        _builder.SetA(hasArguments ? callAddress : address);
        _builder.SwapA_C();
        _builder.SetB(returnAddress);
        _builder.Jump(_callLabel);

        if (hasArguments)
        {
            _builder.Mark(callAddress);

            for (var i = 0; i < arguments!.Count; i++)
            {
                var argument = arguments[i];
                argumentTypes[i] = argument.BuildExpression(this);
                _builder.StoreA(GetStackVariable(i));
            }

            _builder.Jump(address);
        }

        _builder.Mark(returnAddress);

        return argumentTypes;
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

        if (_hasCall || _functions.Count > 0)
        {
            builder.Mark(_stackIndex);
            builder.EmitRaw(0xE000);

            foreach (var (_, function) in _functions)
            {
                builder.Mark(function.Label);
                builder.AddRange(function.Builder._builder);
                builder.Jump(_returnLabel);
            }

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

    public void DeclareFunction(Function statement)
    {
        _functions.Add(statement.Name, statement);
    }
}
