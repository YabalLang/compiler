using Yabal.Exceptions;
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
    private IVariable? _variable;

    public IVariable Variable => _variable ?? throw new InvalidOperationException("Variable not set");

    public override void Initialize(YabalBuilder builder)
    {
        if (builder.TryGetVariable(Identifier.Name, out var variable))
        {
            _variable = variable;
        }
        else
        {
            _variable = builder.GetFunctions(Identifier.Name).FirstOrDefault();
        }

        if (_variable == null)
        {
            throw new InvalidCodeException($"Variable '{Identifier.Name}' not found", Range);
        }

        _variable.AddReference(Identifier);
    }

    public override void BuildExpressionToPointer(YabalBuilder builder, LanguageType suggestedType, Pointer pointer)
    {
        if (Variable.IsDirectReference)
        {
            builder.SetA(Variable.Pointer);
            builder.StoreA(pointer);
        }
        else
        {
            for (var i = 0; i < suggestedType.Size; i++)
            {
                builder.LoadA(Variable.Pointer.Add(i));
                builder.StoreA(pointer.Add(i));
            }
        }
    }

    protected override void BuildExpressionCore(YabalBuilder builder, bool isVoid, LanguageType? suggestedType)
    {
        if (Variable.IsDirectReference)
        {
            builder.SetA(Variable.Pointer);
        }
        else
        {
            builder.LoadA(Variable.Pointer);
        }
    }

    public override void MarkModified()
    {
        if (Variable.ReadOnly)
        {
            throw new InvalidCodeException($"Cannot modify read-only variable {Identifier.Name}", Range);
        }

        Variable.Constant = false;
    }

    void IExpressionToB.BuildExpressionToB(YabalBuilder builder)
    {
        if (Variable.IsDirectReference)
        {
            builder.SetB(Variable.Pointer);
        }
        else
        {
            builder.LoadB(Variable.Pointer);
        }
    }

    public override bool OverwritesB => false;

    public (IVariable, int? Offset) GetVariable(YabalBuilder builder)
    {
        if (Variable.Initializer is IVariableSource variable && Type.StaticType == StaticType.Reference)
        {
            return variable.GetVariable(builder);
        }

        return (Variable, null);
    }

    public bool CanGetVariable => true;

    public override LanguageType Type => _variable?.Type ?? LanguageType.Unknown;

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

    public override void StoreAddressInA(YabalBuilder builder, int offset)
    {
        Variable.AddUsage();

        if (Variable.Type.IsReference || Variable.IsDirectReference)
        {
            builder.LoadA(Variable.Pointer.Add(offset));
        }
        else
        {
            builder.SetA(Variable.Pointer.Add(offset));
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
            Variable.AddUsage();

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

        _variable.MarkUsed();

        switch (Value)
        {
            case int intValue:
                return new IntegerExpression(Range, intValue);
            case bool boolValue:
                return new BooleanExpression(Range, boolValue);
            default:
                _variable.AddUsage();
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
