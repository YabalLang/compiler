namespace Astro8.Instructions;

public class Assembler
{
    private readonly InstructionPointer _tempA;
    private readonly InstructionPointer _tempB;

    private readonly Dictionary<int, InstructionPointer> _values = new();
    private readonly int _valueOffset;

    public Assembler()
    {
        InstructionBuilder = new InstructionBuilder();

        var label = InstructionBuilder.CreateLabel();
        InstructionBuilder.Jump(label);
        _tempA = InstructionBuilder.Nop().CreatePointer();
        _tempB = InstructionBuilder.Nop().CreatePointer();
        _valueOffset = InstructionBuilder.Count;
        label.Mark();
    }

    public InstructionBuilder InstructionBuilder { get; }

    public InstructionPointer CreateValuePointer(int value)
    {
        if (_values.TryGetValue(value, out var pointer))
        {
            return pointer;
        }

        pointer = InstructionBuilder.EmitRawAt(_valueOffset, value);
        _values[value] = pointer;
        return pointer;
    }

    public void Add(string line)
    {
        var tokens = line.Split(null);

        switch (tokens[0])
        {
            case "mov":
                Mov(tokens[1], tokens[2]);
                break;
        }
    }

    public void Mov(string target, string value)
    {
        if (target == "a")
        {
            if (value == "a")
            {
                InstructionBuilder.Nop();
            }
            else if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                InstructionBuilder.StoreC(_tempB);
                InstructionBuilder.SwapA_B();
                InstructionBuilder.StoreA(_tempA);
                InstructionBuilder.LoadB(_tempA);
                InstructionBuilder.LoadC(_tempB);
            }
            else if (value == "c")
            {
                InstructionBuilder.StoreC(_tempA);
                InstructionBuilder.LoadA(_tempA);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt <= InstructionReference.MaxDataLength)
                {
                    InstructionBuilder.SetA(valueInt);
                }
                else
                {
                    InstructionBuilder.LoadA(CreateValuePointer(valueInt));
                }
            }
            else
            {
                throw new Exception("Invalid value");
            }
        }
        else if (target == "b")
        {
            if (value == "a")
            {
                InstructionBuilder.StoreA(_tempA);
                InstructionBuilder.LoadB(_tempA);
            }
            else if (value == "b")
            {
                InstructionBuilder.Nop();
            }
            else if (value == "c")
            {
                InstructionBuilder.StoreC(_tempA);
                InstructionBuilder.LoadB(_tempA);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt < InstructionReference.MaxDataLength)
                {
                    InstructionBuilder.SetB(valueInt);
                }
                else
                {
                    InstructionBuilder.LoadB(CreateValuePointer(valueInt));
                }
            }
            else
            {
                throw new Exception("Invalid value");
            }
        }
        else if (target == "c")
        {
            if (value == "a")
            {
                InstructionBuilder.StoreA(_tempA);
                InstructionBuilder.LoadC(_tempA);
            }
            else if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                InstructionBuilder.SwapA_B();
                InstructionBuilder.StoreA(_tempA);
                InstructionBuilder.SwapA_B();
                InstructionBuilder.LoadC(_tempA);
            }
            else if (value == "b")
            {
                InstructionBuilder.Nop();
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt < InstructionReference.MaxDataLength)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    InstructionBuilder.LoadC(CreateValuePointer(valueInt));
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public int[] ToArray() => InstructionBuilder.ToArray();
}
