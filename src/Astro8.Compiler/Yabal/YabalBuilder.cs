using System.Diagnostics.CodeAnalysis;
using System.Security.AccessControl;
using Antlr4.Runtime;
using Astro8.Yabal;
using Astro8.Yabal.Ast;
using Astro8.Yabal.Visitor;

namespace Astro8.Instructions;

public class YabalBuilder : InstructionBuilderBase, IProgram
{
    private readonly YabalBuilder? _parent;
    private readonly InstructionBuilder _builder;

    private readonly Dictionary<int, InstructionPointer> _values;
    private readonly InstructionPointer _stackPointer;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly Stack<BlockStack> _blockStack;
    private readonly List<InstructionPointer> _stack;
    private readonly Dictionary<string, Function> _functions = new();
    private readonly Dictionary<string, InstructionPointer> _globals;
    private readonly List<InstructionPointer> _temporaryPointers;
    private bool _hasCall;

    public YabalBuilder()
    {
        _builder = new InstructionBuilder();

        _callLabel = _builder.CreateLabel("__call");
        _returnLabel = _builder.CreateLabel("__return");

        ReturnValue = _builder.CreatePointer(name: "Yabal:return_value");

        _stackPointer = new InstructionPointer(name: "Yabal:stack_pointer");
        _blockStack = new Stack<BlockStack>();
        _stack = new List<InstructionPointer>();
        _values = new Dictionary<int, InstructionPointer>();
        _globals = new Dictionary<string, InstructionPointer>();
        _temporaryPointers = new List<InstructionPointer>();

        PushBlock(true);
    }

    public YabalBuilder(YabalBuilder parent)
    {
        _parent = parent;
        _builder = new InstructionBuilder();

        _callLabel = parent._callLabel;
        _returnLabel = parent._returnLabel;

        ReturnValue = parent.ReturnValue;

        _stackPointer = parent._stackPointer;
        _blockStack = parent._blockStack;
        _stack = parent._stack;
        _values = parent._values;
        _globals = parent._globals;
        _temporaryPointers = parent._temporaryPointers;
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

    public InstructionPointer GetOrCreateGlobalVariable(string name)
    {
        if (_globals.TryGetValue(name, out var pointer))
        {
            return pointer;
        }

        pointer = new InstructionPointer($"Global:{name}");
        _globals[name] = pointer;
        return pointer;
    }

    public InstructionPointer ReturnValue { get; set; }

    public BlockStack Block => _blockStack.Peek();

    private void PushBlock(bool isGlobal, FunctionDeclarationStatement? function = null)
    {
        _blockStack.Push(new BlockStack
        {
            IsGlobal = isGlobal,
            Function = function
        });
    }

    public void PushBlock(FunctionDeclarationStatement function)
    {
        PushBlock(false, function);
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
        builder.LoadA(_stackPointer);
        builder.StoreB_ToAddressInA();

        // Store values on stack
        foreach (var pointer in _stack)
        {
            builder.SetB(1);
            builder.Add();

            builder.LoadB(pointer);
            builder.StoreB_ToAddressInA();
        }

        // Increment stack pointer
        builder.LoadA(_stackPointer);
        builder.SetB(_stack.Count + 1);
        builder.Add();
        builder.StoreA(_stackPointer);

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
        var setArguments = _builder.CreateLabel();

        _builder.SetA(hasArguments ? setArguments : address);
        _builder.SwapA_C();
        _builder.SetB(returnAddress);
        _builder.Jump(_callLabel);

        if (hasArguments)
        {
            _builder.Mark(setArguments);

            for (var i = 0; i < arguments!.Count; i++)
            {
                var argument = arguments[i];
                argumentTypes[i] = argument.BuildExpression(this, false);
                _builder.StoreA(GetStackVariable(i));
            }

            _builder.Jump(address);
        }

        _builder.Mark(returnAddress);
        _builder.LoadA(ReturnValue);

        return argumentTypes;
    }

    private void CreateReturn(InstructionBuilderBase builder)
    {
        builder.Mark(_returnLabel);

        // Store return value
        builder.StoreA(ReturnValue);

        // Decrement the stack pointer
        builder.LoadA(_stackPointer);
        builder.SetB(_stack.Count + 1);
        builder.Sub();
        builder.StoreA(_stackPointer);

        // Restore values from the stack
        builder.SetB(1);

        foreach (var pointer in _stack)
        {
            builder.Add();
            builder.LoadA_FromAddressUsingA();
            builder.StoreA(pointer);
        }

        // Go to the return address
        builder.LoadA(_stackPointer);
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

        foreach (var (value, pointer) in _values)
        {
            builder.Mark(pointer);
            builder.EmitRaw(value);
        }

        foreach (var (_, pointer) in _globals)
        {
            builder.Mark(pointer);
            builder.EmitRaw(0);
        }

        foreach (var pointer in _stack)
        {
            builder.Mark(pointer);
            builder.EmitRaw(0);
        }

        foreach (var pointer in _temporaryPointers)
        {
            builder.Mark(pointer);
            builder.EmitRaw(0);
        }

        if (_hasCall || _functions.Count > 0)
        {
            builder.Mark(ReturnValue);
            builder.EmitRaw(0);

            builder.Mark(_stackPointer);
            builder.EmitRaw(0xE000);

            foreach (var (_, function) in _functions)
            {
                builder.Mark(function.Label);
                builder.AddRange(function.Builder._builder);
                builder.Jump(_returnLabel);
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

    public TemporaryVariable GetTemporaryVariable()
    {
        var block = Block;

        if (block.TemporaryVariablesStack.Count > 0)
        {
            return block.TemporaryVariablesStack.Pop();
        }

        var pointer = block.IsGlobal
            ? new InstructionPointer("Temp:" + _temporaryPointers.Count)
            : GetStackVariable(block.StackOffset++);

        if (block.IsGlobal)
        {
            _temporaryPointers.Add(pointer);
        }

        return new TemporaryVariable(pointer, Block);
    }
}


public sealed class TemporaryVariable : IDisposable
{
    public TemporaryVariable(InstructionPointer pointer, BlockStack block)
    {
        Pointer = pointer;
        Block = block;
    }

    public InstructionPointer Pointer { get; }

    public BlockStack Block { get; }

    public void Dispose()
    {
        Block.TemporaryVariablesStack.Push(this);
    }

    public static implicit operator InstructionPointer(TemporaryVariable variable)
    {
        return variable.Pointer;
    }

    public static implicit operator PointerOrData(TemporaryVariable variable)
    {
        return variable.Pointer;
    }
}
