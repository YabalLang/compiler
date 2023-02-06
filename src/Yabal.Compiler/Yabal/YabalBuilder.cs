using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Yabal.Ast;
using Yabal.Exceptions;
using Yabal.Instructions;
using Yabal.Visitor;
using Zio;
using Zio.FileSystems;

namespace Yabal;

public class YabalBuilder : InstructionBuilderBase, IProgram
{
    private readonly TypeVisitor _visitor;
    private readonly YabalBuilder? _parent;
    private readonly InstructionBuilder _builder;

    private readonly InstructionPointer _tempPointer;
    private readonly InstructionPointer _stackPointer;
    private readonly InstructionPointer _stackAllocPointer;
    private readonly InstructionLabel _callLabel;
    private readonly InstructionLabel _returnLabel;
    private readonly Dictionary<string, List<Function>> _functions = new();
    private readonly Dictionary<string, InstructionPointer> _strings;
    private readonly Dictionary<(string, FileType type), InstructionPointer> _files;
    private readonly Dictionary<SourceRange, List<CompileError>> _errors;
    private readonly BlockStack _globalBlock;

    public YabalBuilder()
    {
        _builder = new InstructionBuilder();

        _callLabel = _builder.CreateLabel("__call");
        _returnLabel = _builder.CreateLabel("__return");

        ReturnValue = _builder.CreatePointer(name: "Yabal:return_value");

        _tempPointer = _builder.CreatePointer(name: "Yabal:temp");
        _stackPointer = new InstructionPointer(name: "Yabal:stack_pointer");
        _stackAllocPointer = new InstructionPointer(name: "Yabal:stack_alloc_pointer");
        Stack = new PointerCollection("Stack");
        Globals = new PointerCollection("Global");
        Temporary = new PointerCollection("Temp");
        _strings = new Dictionary<string, InstructionPointer>();
        _files = new Dictionary<(string, FileType), InstructionPointer>();
        _errors = new Dictionary<SourceRange, List<CompileError>>();
        BinaryOperators = new Dictionary<(BinaryOperator, LanguageType, LanguageType), Function>();

        _visitor = new TypeVisitor();
        _globalBlock = new BlockStack { IsGlobal = true };
        Block = _globalBlock;
    }

    public YabalBuilder(YabalBuilder parent)
    {
        _parent = parent;
        _visitor = parent._visitor;
        _builder = new InstructionBuilder();

        _callLabel = parent._callLabel;
        _returnLabel = parent._returnLabel;

        ReturnValue = parent.ReturnValue;

        _stackPointer = parent._stackPointer;
        _stackAllocPointer = parent._stackAllocPointer;
        _tempPointer = parent._tempPointer;
        Block = parent.Block;
        Stack = parent.Stack;
        Globals = parent.Globals;
        Temporary = parent.Temporary;
        _globalBlock = parent._globalBlock;
        _strings = parent._strings;
        _files = parent._files;
        _errors = parent._errors;
        BinaryOperators = parent.BinaryOperators;
    }

    public Dictionary<(BinaryOperator, LanguageType, LanguageType), Function> BinaryOperators { get; } = new();

    public InstructionPointer StackAllocPointer => _stackAllocPointer;

    public List<Variable> Variables { get; } = new();

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

    public BlockStack PushBlock(ScopeStatement? function = null)
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

    public void SetBlock(BlockStack block)
    {
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

    public Variable GetVariable(string name, SourceRange? range = default)
    {
        if (!TryGetVariable(name, out var variable))
        {
            throw new InvalidCodeException($"Variable '{name}' not found", range);
        }

        return variable;
    }

    public Variable CreateVariable(Identifier name, LanguageType type, Expression? initializer = null)
    {
        var pointer = Block.IsGlobal
            ? Globals.GetNext(Block, type)
            : Stack.GetNext(Block, type);

        var variable = new Variable(name, pointer, type, initializer, Block.IsGlobal);
        Block.DeclareVariable(name.Name, variable);
        pointer.AssignedVariables.Add(variable);
        Variables.Add(variable);
        return variable;
    }

    public bool TryGetFunction(string name, LanguageType[] types, [NotNullWhen(true)] out Function? function)
    {
        if (_functions.TryGetValue(name, out var functions))
        {
            function = functions.FirstOrDefault(i =>
                types.Length >= i.RequiredParameterCount &&
                types.Zip(i.Parameters, (a, b) => a == b.Type).All(b => b));

            if (function != null)
            {
                return true;
            }

            function = functions.FirstOrDefault(i =>
                types.Length >= i.RequiredParameterCount &&
                types.Zip(i.Parameters, (a, b) => a.Size == b.Type.Size).All(b => b));

            if (function != null)
            {
                AddError(ErrorLevel.Warning, function.Range, $"Function {function} is being called with a different types ({string.Join(", ", types.Select(i => i.ToString()))}), it's suggested to make an overload");
                return true;
            }
        }

        if (_parent != null)
        {
            return _parent.TryGetFunction(name, types, out function);
        }

        function = default;
        return false;
    }

    public Function GetFunction(string name, LanguageType[] types, SourceRange range)
    {
        if (!TryGetFunction(name, types, out var function))
        {
            throw new InvalidCodeException($"Function {name}({string.Join(", ", types.Select(i => i.ToString()))}) not found", range);
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

    public async ValueTask<bool> CompileCodeAsync(string code, bool optimize = true, Uri? file = null, IFileSystem? fileSystem = null)
    {
        fileSystem ??= new PhysicalFileSystem();
        file ??= new Uri("memory://");

        try
        {
            var program = Parse(fileSystem, file, code);

            program.Declare(this);
            program.Initialize(this);

            foreach (var (path, type) in _files.Keys)
            {
                await FileContent.LoadAsync(path, type);
            }

            if (optimize)
            {
                program = program.Optimize();
            }

            program.Build(this);

            return Errors.SelectMany(x => x.Value).All(x => x.Level != ErrorLevel.Error);
        }
        catch (ParseCanceledException e) when (e.InnerException is InputMismatchException innerException)
        {
            AddError(ErrorLevel.Error, SourceRange.From(innerException.OffendingToken, file), "Unexpected token");
            return false;
        }
        catch (ParseCanceledException e) when (e.InnerException is NoViableAltException innerException)
        {
            AddError(ErrorLevel.Error, SourceRange.From(innerException.StartToken, file), "Unexpected token");
            return false;
        }
        catch (InvalidCodeException e)
        {
            AddError(ErrorLevel.Error, e.Range ?? default, e.Message);
            return false;
        }
    }

    public ProgramStatement Parse(IFileSystem fileSystem, Uri file, string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new YabalLexer(inputStream);

        var commonTokenStream = new CommonTokenStream(lexer);
        var parser = new YabalParser(commonTokenStream)
        {
            ErrorHandler = new BailErrorStrategy(),
        };

        var listener = new YabalVisitor(file, fileSystem);
        return listener.VisitProgram(parser.program());
    }

    private void CreateCall(InstructionBuilderBase builder)
    {
        // B = Return address
        // C = Method address

        builder.Mark(_callLabel);

        // Store return address
        builder.LoadA(_stackPointer);
        builder.StoreB_ToAddressInA();

        foreach (var pointer in Stack)
        {
            for (var i = 0; i < pointer.Size; i++)
            {
                builder.SetB(1);
                builder.Add();

                builder.LoadB(pointer.Add(i));
                builder.StoreB_ToAddressInA();
            }
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
        var hasArguments = arguments is { Count: > 0 };
        var hasReferenceArguments = arguments?.Any(a => a is IVariableSource) ?? false;
        var returnAddress = _builder.CreateLabel();
        var setArguments = _builder.CreateLabel();

        _builder.SetA_Large(hasArguments ? setArguments : address);
        _builder.SwapA_C();

        using var currentStackPointer = GetTemporaryVariable(global: true);

        if (hasReferenceArguments)
        {
            _builder.LoadA(_stackPointer);
            _builder.StoreA(currentStackPointer);
            _builder.SetComment("store current stack pointer");
        }

        _builder.SetA_Large(returnAddress);
        _builder.StoreA(_tempPointer);
        _builder.LoadB(_tempPointer);

        _builder.Jump(_callLabel);

        if (hasArguments)
        {
            _builder.Mark(setArguments);

            var sizes = new Dictionary<int, int>();

            for (var i = 0; i < arguments!.Count; i++)
            {
                var argument = arguments[i];
                var size = argument.Type.Size;

                if (!sizes.TryGetValue(size, out var offset))
                {
                    offset = 0;
                }

                var variable = Stack.Get(offset, argument.Type.Size);

                if (argument is IVariableSource { CanGetVariable: true } source)
                {
                    var (sourceVariable, sourceOffset) = source.GetVariable(this);

                    var stackVariable = Stack.FirstOrDefault(s => s.AssignedVariables.Contains(sourceVariable));
                    var stackOffset = 1 + (sourceOffset ?? 0) + Stack.TakeWhile(s => s != stackVariable).Sum(item => item.Size);

                    if (argument.Type.StaticType == StaticType.Reference)
                    {
                        if (sourceVariable.IsGlobal)
                        {
                            _builder.SetA(sourceVariable.Pointer);
                            _builder.StoreA(variable);
                        }
                        else if (sourceVariable.Type.StaticType == StaticType.Reference)
                        {
                            _builder.LoadA(sourceVariable.Pointer);
                            _builder.StoreA(variable);
                        }
                        else if (stackVariable != null)
                        {
                            _builder.LoadA(currentStackPointer);
                            _builder.SetB(stackOffset);
                            _builder.Add();

                            _builder.StoreA(variable);
                            _builder.SetComment("store stack pointer as reference");
                        }
                        else
                        {
                            _builder.SetA(sourceVariable.Pointer);
                            _builder.StoreA(variable);
                        }
                    }
                    else if (!sourceVariable.IsGlobal)
                    {
                        for (var j = 0; j < size; j++)
                        {
                            _builder.LoadA(currentStackPointer);
                            _builder.SetB(stackOffset + j);
                            _builder.Add();

                            _builder.LoadA_FromAddressUsingA();
                            _builder.StoreA(variable.Add(j));
                        }
                    }
                    else
                    {
                        SetValue(variable, argument.Type, argument);
                    }
                }
                else if (argument.Type.StaticType == StaticType.Reference)
                {
                    throw new InvalidCodeException("Could not create a reference from the given argument", argument.Range);
                }
                else
                {
                    SetValue(variable, argument.Type, argument);
                }

                sizes[size] = offset + 1;
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
        _builder.Jump(_returnLabel);
    }

    public int? Index
    {
        get => _builder.Index;
        set => _builder.Index = value;
    }

    public override int Count => _builder.Count;

    public int DisallowC
    {
        get => _builder.DisallowC;
        set => _builder.DisallowC = value;
    }

    public IEnumerable<Function> Functions => _functions.SelectMany(i => i.Value);

    public bool HasStackAllocation { get; set; }

    public LanguageType? ReturnType { get; set; }

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

    public void CopyTo(int[] array)
    {
        Build().CopyTo(array);
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
        var hasFunction = Functions.Where(i => !i.Inline).Concat(BinaryOperators.Values).Any();

        if (Globals.Count == 0 &&
            Temporary.Count == 0 &&
            Stack.Count == 0 &&
            !hasFunction)
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

        builder.Mark(ReturnValue);

        for (var i = 0; i < 10; i++)
        {
            builder.EmitRaw(0, $"return value ({i})");
        }

        if (hasFunction)
        {
            var stackAllocStart = 0xEF6E;
            var stackStart = stackAllocStart - (1 + Stack.Sum(i => i.Size) * 16);

            builder.Mark(_stackPointer);
            builder.EmitRaw(stackStart, "stack pointer");

            builder.Mark(_stackAllocPointer);
            builder.EmitRaw(stackAllocStart, "stack allocation pointer");

            builder.Mark(_tempPointer);
            builder.EmitRaw(0, "temporary pointer");

            CreateCall(builder);
            CreateReturn(builder);
        }

        foreach (var function in Functions.Concat(BinaryOperators.Values))
        {
            if (function.References.Count == 0)
            {
                AddError(ErrorLevel.Debug, function.Name.Left?.Range ?? default, $"Function '{function.Name}' is never called and will be excluded from the output.");
                continue;
            }

            if (function.Inline)
            {
                continue;
            }

            builder.Mark(function.Label);
            builder.AddRange(function.Builder._builder);
            builder.Jump(_returnLabel);
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

    public void DeclareFunction(Function function)
    {
        if (function.Name.IsLeft)
        {
            var identifier = function.Name.Left;

            if (!_functions.TryGetValue(identifier.Name, out var functions))
            {
                functions = new List<Function>();
                _functions.Add(identifier.Name, functions);
            }

            if (functions.Any(i => i.Parameters.Count == function.Parameters.Count && i.Parameters.Zip(function.Parameters).All(j => j.First.Type == j.Second.Type)))
            {
                throw new InvalidCodeException($"Function '{identifier.Name}' with the same parameters already exists.", identifier.Range);
            }

            functions.Add(function);
        }
        else
        {
            var op = function.Name.Right;

            if (function.Parameters.Count != 2)
            {
                throw new InvalidCodeException("Binary operator must have 2 parameters.", function.Range);
            }

            BinaryOperators.Add((op, function.Parameters[0].Type, function.Parameters[1].Type), function);
        }
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

    public void SetValue(Pointer pointer, LanguageType type, Expression expression)
    {
        if (expression is InitStructExpression initStruct)
        {
            InitStruct(type, pointer, initStruct);
            return;
        }

        var size = expression.Type.Size;

        switch (expression)
        {
            case AddressExpression { DirectCopy: true } addressExpression when addressExpression.Type.StaticType == StaticType.Pointer:
            {
                if (addressExpression is IdentifierExpression {Variable: var variable})
                {
                    _builder.SetA(pointer);
                    _builder.LoadB(variable.Pointer);
                    _builder.StoreB_ToAddressInA();

                    _builder.SetA(pointer.Add(1));
                    _builder.LoadB(variable.Pointer.Add(1));
                    _builder.StoreB_ToAddressInA();
                }
                else if (addressExpression.Pointer is { } valuePointer)
                {
                    _builder.SetB_Large(valuePointer);
                    _builder.StoreB_ToAddressInA();

                    _builder.SetA(pointer.Add(1));
                    _builder.SetB(valuePointer.Bank);
                    _builder.StoreB_ToAddressInA();
                }
                else
                {
                    throw new NotImplementedException();
                }

                break;
            }
            case AddressExpression { DirectCopy: true, Pointer: { } valuePointer }:
            {
                for (var i = 0; i < size; i++)
                {
                    valuePointer.CopyTo(this, pointer, i);
                }

                break;
            }
            case AddressExpression { DirectCopy: true } addressExpression:
            {
                for (var i = 0; i < size; i++)
                {
                    addressExpression.StoreAddressInA(this);

                    if (i > 0)
                    {
                        SetB(i);
                        Add();
                    }

                    LoadA_FromAddressUsingA();
                    StorePointer(pointer.Add(i));
                }

                break;
            }
            case {} when expression.Type is { StaticType: StaticType.Pointer, IsReference: true } && size == 2:
            {
                expression.BuildExpression(this, false);
                StorePointer(pointer);

                SetA(0);
                StorePointer(pointer.Add(1));
                break;
            }
            case StringExpression when type.StaticType == StaticType.Pointer:
            {
                expression.BuildExpression(this, false);
                StorePointer(pointer);

                SetA(0);
                StorePointer(pointer.Add(1));
                break;
            }
            default:
            {
                if (size == 1)
                {
                    expression.BuildExpression(this, false);
                    StorePointer(pointer);
                }
                else
                {
                    throw new NotImplementedException();
                }

                break;
            }
        }

    }

    public void StorePointer(Pointer pointer)
    {
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

            SwapA_B();
        }
    }

    public void InitStruct(LanguageType type, Pointer pointer, InitStructExpression initStruct)
    {
        if (type is not { StaticType: StaticType.Struct, StructReference: { } structRef })
        {
            throw new InvalidOperationException();
        }

        for (var i = 0; i < initStruct.Items.Count; i++)
        {
            var (name, expression) = initStruct.Items[i];
            var range = name?.Range ?? expression.Range;
            var field = name != null
                ? structRef.Fields.FirstOrDefault(f => f.Name == name.Name)
                : structRef.Fields[i];

            if (field == null)
            {
                AddError(ErrorLevel.Error, range, name != null
                    ? $"Struct {structRef.Name} does not have a field named '{name.Name}'."
                    : $"Struct {structRef.Name} does not have a field at index {i}.");
                continue;
            }

            var fieldPointer = pointer.Add(field.Offset);

            if (field.Bit is { } bit)
            {
                StoreBit(fieldPointer, expression, bit);
            }
            else
            {
                SetValue(fieldPointer, field.Type, expression);
            }
        }
    }

    public void StoreBit(Either<Pointer, AddressExpression> target, Expression expression, Bit bit)
    {
        if (expression is IConstantValue {Value: int intValue})
        {
            var bits = (1 << bit.Size) - 1;

            LoadA(target);

            SetB_Large(~(bits << bit.Offset) & 0xFFFF);
            And();

            SetB_Large((intValue & bits) << bit.Offset);
            Or();

            StoreA(target);
        }
        else
        {
            expression.BuildExpression(this, false);
            StoreBitInA(target, bit);
        }
    }

    private void StoreA(Either<Pointer, AddressExpression> target)
    {
        if (target.IsLeft)
        {
            target.Left.StoreA(this);
        }
        else
        {
            SwapA_B();
            target.Right.StoreAddressInA(this);
            StoreB_ToAddressInA();
        }
    }

    private void LoadA(Either<Pointer, AddressExpression> target)
    {
        if (target.IsLeft)
        {
            target.Left.LoadToA(this);
        }
        else
        {
            target.Right.StoreAddressInA(this);
            LoadA_FromAddressUsingA();
        }
    }

    public void StoreBitInA(Either<Pointer, AddressExpression> target, Bit bit)
    {
        using var value = GetTemporaryVariable(global: true);
        var bits = (1 << bit.Size) - 1;

        // Remove invalid bits
        if (bits > InstructionReference.MaxData)
        {
            SetB_Large(bits);
        }
        else
        {
            SetB(bits);
        }
        And();

        // Move to correct position
        if (bit.Offset > 0)
        {
            SetB(bit.Offset);
            BitShiftLeft();
        }

        StoreA(value);

        // Get current value
        LoadA(target);
        SwapA_B();

        // Clear rest of the bits
        SetA_Large(~(bits << bit.Offset) & 0xFFFF);
        And();

        LoadB(value);
        Or();

        // Store the result
        StoreA(target);
    }
}
