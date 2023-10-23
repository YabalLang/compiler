using Yabal.Visitor;

namespace Yabal.Ast;

public record Namespace(IReadOnlyList<string> Namespaces)
{
    public static readonly Namespace Global = new(Array.Empty<string>());

    public bool Contains(IEnumerable<Namespace> ns) => Namespaces.Count == 0 || ns.Any(Contains);

    public bool Contains(Namespace ns)
    {
        if (Namespaces.Count == 0)
        {
            return true;
        }

        return Namespaces.Count <= ns.Namespaces.Count && Namespaces.SequenceEqual(ns.Namespaces.Take(Namespaces.Count));
    }

    public virtual bool Equals(Namespace? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Namespaces.SequenceEqual(other.Namespaces);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Namespaces);
    }

    public override string ToString()
    {
        return string.Join(".", Namespaces);
    }
}

public record Identifier(SourceRange Range, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}

public record IdentifierExpression(SourceRange Range, Identifier Identifier) : AddressExpression(Range), IExpressionToB, IConstantValue, IVariableSource
{
    private Variable? _variable;

    public Variable Variable => _variable ?? throw new InvalidOperationException("Variable not set");

    public override void Initialize(YabalBuilder builder)
    {
        _variable = builder.GetVariable(Identifier.Name, Identifier.Range);
        _variable.References.Add(this);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        for (var i = 0; i < suggestedType.Size; i++)
        {
            builder.LoadA(Variable.Pointer.Add(i));
            builder.StoreA(pointer, i);
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        builder.LoadA(Variable.Pointer);
    }

    public override void MarkModified()
    {
        Variable.Constant = false;
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
    {
        builder.LoadB(Variable.Pointer);
    }

    public override bool OverwritesB => false;

    public (Variable, int? Offset) GetVariable(YabalBuilder builder)
    {
        if (Variable.Initializer is IVariableSource variable && Type.StaticType == StaticType.Reference)
        {
            return variable.GetVariable(builder);
        }

        return (Variable, null);
    }

    public bool CanGetVariable => true;

    public override LanguageType Type => Variable.Type;

    bool IExpressionToB.OverwritesA => false;

    public override int? Bank
    {
        get
        {
            if (Variable.Type.IsReference)
            {
                return 0;
            }

            return Pointer?.Bank;
        }
    }

    public override void StoreAddressInA(YabalBuilder builder)
    {
        Variable.Usages++;

        if (Variable.Type.IsReference)
        {
            builder.LoadA(Variable.Pointer);
        }
        else
        {
            builder.SetA(Variable.Pointer);
        }
    }

    public override string ToString()
    {
        return Identifier.Name;
    }

    public object? Value => _variable is { Constant: true, Initializer: {} initializer }
        ? initializer.Optimize(Type) is IConstantValue { Value: var value }
            ? value
            : null
        : null;

    public bool HasConstantValue => Value is not null;

    public void StoreConstantValue(Span<int> buffer)
    {
        if (Value is int value)
        {
            buffer[0] = value;
            buffer[1] = 0;
        }
        else
        {
            throw new InvalidOperationException("Cannot store a null value.");
        }
    }

    public override Pointer? Pointer
    {
        get
        {
            Variable.Usages++;

            if (Variable.Type.IsReference)
            {
                return null;
            }

            if (Variable.Type.StaticType == StaticType.Pointer)
            {
                return (Value as IAddress)?.Pointer;
            }

            return Variable.Pointer;
        }
    }

    public override Expression Optimize(LanguageType? suggestedType)
    {
        if (_variable == null)
        {
            return this;
        }

        _variable.HasBeenUsed = true;

        switch (Value)
        {
            case int intValue:
                return new IntegerExpression(Range, intValue);
            case bool boolValue:
                return new BooleanExpression(Range, boolValue);
            default:
                _variable.Usages++;
                return this;
        }
    }

    public override IdentifierExpression CloneExpression()
    {
        return new IdentifierExpression(Range, Identifier)
        {
            _variable = _variable
        };
    }
}
