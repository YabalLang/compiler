using System.Diagnostics.CodeAnalysis;
using Yabal.Ast;
using Yabal.Instructions;

namespace Yabal.Visitor;

public class BlockStack
{
    public readonly Stack<TemporaryVariable> TemporaryVariablesStack = new();

    private readonly Dictionary<int, int> _globalOffset = new();
    private readonly Dictionary<int, int> _stackOffset = new();
    private readonly Dictionary<string, Variable> _variables = new();
    private readonly List<Namespace> _using = new();
    private InstructionLabel? _continue;
    private InstructionLabel? _break;
    private InstructionLabel? _return;
    private Namespace? _ns;

    public BlockStack()
    {
    }

    public BlockStack(BlockStack parent, ScopeStatement? function = null)
    {
        IsGlobal = parent.IsGlobal && function == null;
        Scope = parent.Scope ?? function;
        Parent = parent;
        _globalOffset = parent._globalOffset;
        _stackOffset = parent._stackOffset.ToDictionary(x => x.Key, x => x.Value);
    }

    public IReadOnlyDictionary<string, Variable> Variables => _variables;

    public int GetNextOffset(int size, bool global)
    {
        var list = global ? _globalOffset : _stackOffset;

        if (!list.TryGetValue(size, out var offset))
        {
            offset = 0;
        }

        list[size] = offset + 1;
        return offset;
    }

    public bool IsGlobal { get; set; }

    public ScopeStatement? Scope { get; }

    public BlockStack? Parent { get; set; }

    public Dictionary<string, InstructionLabel> Labels { get; } = new();

    public InstructionLabel? Continue
    {
        get => _continue ?? Parent?.Continue;
        set => _continue = value;
    }

    public InstructionLabel? Break
    {
        get => _break ?? Parent?.Break;
        set => _break = value;
    }

    public InstructionLabel? Return
    {
        get => _return ?? Parent?.Return;
        set => _return = value;
    }

    public Namespace Namespace
    {
        get => _ns ?? Parent?._ns ?? Namespace.Global;
        set => _ns = value;
    }

    public IEnumerable<Namespace> EnumerateUsing()
    {
        var current = this;

        while (current != null)
        {
            foreach (var ns in current._using)
            {
                yield return ns;
            }

            current = current.Parent;
        }
    }

    public void AddUsing(Namespace ns)
    {
        _using.Add(ns);
    }

    public void DeclareVariable(string name, Variable variable)
    {
        _variables[name] = variable;
    }

    public bool TryGetVariable(string name, [NotNullWhen(true)] out Variable? variable)
    {
        if (_variables.TryGetValue(name, out variable))
        {
            return true;
        }

        if (Parent != null)
        {
            return Parent.TryGetVariable(name, out variable);
        }

        return false;
    }

    public bool TryGetLabel(string name, [NotNullWhen(true)]  out InstructionLabel? label)
    {
        if (Labels.TryGetValue(name, out label))
        {
            return true;
        }

        if (Parent != null)
        {
            return Parent.TryGetLabel(name, out label);
        }

        return false;
    }
}
