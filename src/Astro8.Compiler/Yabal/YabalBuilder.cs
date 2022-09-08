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

    private readonly InstructionPointer _tempPointer;
    private readonly InstructionPointer _stackPointer;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly Dictionary<string, Function> _functions = new();
    private readonly Dictionary<string, InstructionPointer> _strings;
    private readonly Dictionary<(string, FileType type), InstructionPointer> _files;
    private readonly Dictionary<SourceRange, List<CompileError>> _errors;
    private readonly BlockStack _globalBlock;
    private bool _hasCall;

    public YabalBuilder()
    {
        _builder = new InstructionBuilder();

        _callLabel = _builder.CreateLabel("__call");
        _returnLabel = _builder.CreateLabel("__return");

        ReturnValue = _builder.CreatePointer(name: "Yabal:return_value");

        _tempPointer = _builder.CreatePointer(name: "Yabal:temp");
        _stackPointer = new InstructionPointer(name: "Yabal:stack_pointer");
        Stack = new PointerCollection("Stack");
        Globals = new PointerCollection("Global");
        Temporary = new PointerCollection("Temp");
        _strings = new Dictionary<string, InstructionPointer>();
        _files = new Dictionary<(string, FileType), InstructionPointer>();
        _errors = new Dictionary<SourceRange, List<CompileError>>();

        _globalBlock = new BlockStack { IsGlobal = true };
        Block = _globalBlock;
    }

    public YabalBuilder(YabalBuilder parent)
    {
        _parent = parent;
        _builder = new InstructionBuilder();

        _callLabel = parent._callLabel;
        _returnLabel = parent._returnLabel;

        ReturnValue = parent.ReturnValue;

        _stackPointer = parent._stackPointer;
        _tempPointer = parent._tempPointer;
        Block = parent.Block;
        Stack = parent.Stack;
        Globals = parent.Globals;
        Temporary = parent.Temporary;
        _globalBlock = parent._globalBlock;
        _strings = parent._strings;
        _files = parent._files;
        _errors = parent._errors;
    }

    public PointerCollection Stack { get; }

    public PointerCollection Globals { get; }

    public PointerCollection Temporary { get; }

    public IReadOnlyDictionary<SourceRange, List<CompileError>> Errors => _errors;

    public void AddError(ErrorLevel level, SourceRange range, string error)
    {
        var errorInstance = new CompileError(range, level, error);

        if (_errors.TryGetValue(range, out var list))
        {
            list.Add(errorInstance);
        }
        else
        {
            _errors.Add(range, new List<CompileError> { errorInstance });
        }
    }

    public InstructionPointer GetFile(string path, FileType type)
    {
        var pointer = _builder.CreatePointer(name: $"File:{type}:{_files.Count}");
        _files.Add((path, type), pointer);
        return pointer;
    }

    public InstructionPointer ReturnValue { get; set; }

    public BlockStack Block { get; private set; }

    public BlockStack PushBlock(FunctionDeclarationStatement? function = null)
    {
        return Block = new BlockStack(Block, function);
    }

    public void PopBlock()
    {
        Block = Block.Parent ?? _globalBlock;
    }

    public void PushBlock(BlockStack block)
    {
        block.Parent = Block;
        Block = block;
    }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
    {
        if (Block.TryGetVariable(name, out variable))
        {
            return true;
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

    public Variable CreateVariable(string name, LanguageType type, Expression? initializer = null)
    {
        var pointer = Block.IsGlobal
            ? Globals.GetNext(Block, type)
            : Stack.GetNext(Block, type);

        var variable = new Variable(name, pointer, type, initializer);
        Block.DeclareVariable(name, variable);
        pointer.AssignedVariables.Add(variable);
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

    public InstructionPointer GetString(string value)
    {
        if (_strings.TryGetValue(value, out var pointer))
        {
            return pointer;
        }

        pointer = _builder.CreatePointer($"String:{value}");
        _strings.Add(value, pointer);
        return pointer;
    }

    public async ValueTask CompileCodeAsync(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new YabalLexer(inputStream);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new YabalParser(commonTokenStream)
        {
            // ErrorHandler = new BailErrorStrategy(),
        };

        try
        {
            var listener = new YabalVisitor();
            var program = listener.VisitProgram(parser.program());

            foreach (var (path, type) in listener.Files)
            {
                await FileContent.LoadAsync(path, type);
            }

            program.Declare(this);
            program.Initialize(this);
            program = program.Optimize();
            program.Build(this);
        }
        catch
        {
            throw;
        }
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
        foreach (var pointer in Stack)
        {
            builder.SetB(1);
            builder.Add();

            builder.LoadB(pointer);
            builder.StoreB_ToAddressInA();
        }

        // Increment stack pointer
        builder.LoadA(_stackPointer);
        builder.SetB(Stack.Count + 1);
        builder.Add();
        builder.StoreA(_stackPointer);

        // Go to the address
        builder.SwapA_C();
        builder.JumpToA();
    }

    public void Call(PointerOrData address, IReadOnlyList<Expression>? arguments = null)
    {
        _hasCall = true;

        var hasArguments = arguments is { Count: > 0 };
        var returnAddress = _builder.CreateLabel();
        var setArguments = _builder.CreateLabel();

        _builder.SetA_Large(hasArguments ? setArguments : address);
        _builder.SwapA_C();

        _builder.SetA_Large(returnAddress);
        _builder.StoreA(_tempPointer);
        _builder.LoadB(_tempPointer);

        _builder.Jump(_callLabel);

        if (hasArguments)
        {
            _builder.Mark(setArguments);

            for (var i = 0; i < arguments!.Count; i++)
            {
                var argument = arguments[i];
                var variable = Stack.Get(i, argument.Type.Size);

                argument.BuildExpression(this, false);
                _builder.StoreA(variable);
            }

            _builder.Jump(address);
        }

        _builder.Mark(returnAddress);
        _builder.LoadA(ReturnValue);
    }

    private void CreateReturn(InstructionBuilderBase builder)
    {
        builder.Mark(_returnLabel);

        // Store return value
        builder.StoreA(ReturnValue);

        // Decrement the stack pointer
        builder.LoadA(_stackPointer);
        builder.SetB(Stack.Count + 1);
        builder.Sub();
        builder.StoreA(_stackPointer);

        // Restore values from the stack
        builder.StoreA(_tempPointer);

        foreach (var pointer in Stack)
        {
            builder.LoadA(_tempPointer);
            builder.SetB(1);
            builder.Add();
            builder.StoreA(_tempPointer);

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

    public override void EmitRaw(PointerOrData value, string? comment = null)
    {
        _builder.EmitRaw(value, comment);
    }

    public void CopyTo(int[] array, int offset)
    {
        var builder = Build(offset);
        builder.CopyTo(array);
    }

    public void ToAssembly(StreamWriter writer, bool addComments = false)
    {
        Build().ToAssembly(writer, addComments);
    }

    public void ToHex(StreamWriter writer)
    {
        Build().ToHex(writer);
    }

    public void ToLogisimFile(StreamWriter writer, int minSize = 0)
    {
        Build().ToLogisimFile(writer, minSize);
    }

    public override string ToString()
    {
        return _builder.ToString();
    }

    public InstructionBuildResult Build(int offset = 0)
    {
        var builder = new InstructionBuilder();

        AddHeader(builder);
        builder.AddRange(_builder);
        AddStrings(builder);

        return builder.Build(offset);
    }

    private void AddHeader(InstructionBuilder builder)
    {
        if (Globals.Count == 0 &&
            Temporary.Count == 0 &&
            Stack.Count == 0 &&
            !_hasCall)
        {
            return;
        }

        var programLabel = builder.CreateLabel("Program");

        builder.Jump(programLabel);

        foreach (var pointer in Globals.Concat(Temporary).Concat(Stack))
        {
            builder.Mark(pointer);

            for (var i = 0; i < pointer.Size; i++)
            {
                builder.EmitRaw(0);

                if (i == 0)
                {
                    builder.SetComment($"size: {pointer.Size}, variables: {pointer.AssignedVariableNames}");
                }
            }
        }

        if (_hasCall)
        {
            builder.Mark(ReturnValue);
            builder.EmitRaw(0, "return value");

            builder.Mark(_stackPointer);
            builder.EmitRaw(0xEF6E - (1 + (Stack.Count * 16)), "stack pointer");

            builder.Mark(_tempPointer);
            builder.EmitRaw(0, "temporary pointer");

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
    }

    private void AddStrings(InstructionBuilder builder)
    {
        if (_strings.Count == 0 && _files.Count == 0)
        {
            return;
        }

        var endLabel = builder.CreateLabel("End");
        builder.Jump(endLabel);

        foreach (var (value, pointer) in _strings)
        {
            builder.Mark(pointer);

            for (var i = 0; i < value.Length; i++)
            {
                if (Character.CharToInt.TryGetValue(value[i], out var intValue))
                {
                    builder.EmitRaw(intValue);
                }
                else
                {
                    builder.EmitRaw(0);
                    AddError(ErrorLevel.Error, SourceRange.Zero, ErrorMessages.InvalidCharacterInString(value[i], value));
                }

                if (i == 0)
                {
                    builder.SetComment($"string '{value}'");
                }
            }

            builder.EmitRaw(0);
        }

        foreach (var ((file, type), pointer) in _files)
        {
            var (offset, content) = FileContent.Get(file, type);

            for (var i = 0; i < content.Length; i++)
            {
                if (i == offset)
                {
                    builder.Mark(pointer);
                }

                builder.EmitRaw(content[i]);
            }
        }

        builder.Mark(endLabel);
    }

    public void DeclareFunction(Function statement)
    {
        _functions.Add(statement.Name, statement);
    }

    public void SetComment(string comment)
    {
        _builder.SetComment(comment);
    }

    public TemporaryVariable GetTemporaryVariable(bool global = false, int size = 1)
    {
        var block = global ? _globalBlock : Block;

        if (block.TemporaryVariablesStack.Count > 0)
        {
            return block.TemporaryVariablesStack.Pop();
        }

        var pointer = block.IsGlobal
            ? Temporary.GetNext(size)
            : Stack.GetNext(block, size);

        return new TemporaryVariable(pointer, Block);
    }

    public void SetValue(Pointer pointer, Expression expression)
    {
        var size = expression.Type.Size;

        if (expression is IAddressExpression addressExpression)
        {
            if (addressExpression.Pointer is { } valuePointer)
            {
                for (var i = 0; i < size; i++)
                {
                    valuePointer.CopyTo(this, pointer, i);
                }
            }
            else
            {
                for (var i = 0; i < size; i++)
                {
                    addressExpression.StoreAddressInA(this);
                    SetB(i);
                    Add();
                    LoadA_FromAddressUsingA();

                    StoreA_Large(pointer.Add(i));
                }
            }
        }
        else if (size == 1)
        {
            expression.BuildExpression(this, false);

            if (pointer.Bank == 0)
            {
                StoreA_Large(pointer);
            }
            else
            {
                SwapA_B();
                SetA_Large(pointer);

                SetBank(pointer.Bank);
                StoreB_ToAddressInA();
                SetBank(0);
            }

        }
        else
        {
            throw new NotImplementedException();
        }

    }
}
