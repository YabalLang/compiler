namespace Astro8.Instructions;

public class Assembler
{
    private readonly InstructionBuilder _builder;
    private readonly InstructionPointer _tempA;
    private readonly InstructionPointer _tempB;
    private readonly InstructionPointer _tempC;

    public Assembler()
    {
        _builder = new InstructionBuilder();

        var label = _builder.CreateLabel();
        _builder.Jump(label);
        _tempA = _builder.Nop().CreatePointer();
        _tempB = _builder.Nop().CreatePointer();
        _tempC = _builder.Nop().CreatePointer();
        label.Mark();
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
                _builder.Nop();
            }
            else if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                _builder.StoreC(_tempC);
                _builder.SwapA_B();
                _builder.StoreA(_tempA);
                _builder.LoadB(_tempA);
                _builder.LoadC(_tempC);
            }
            else if (value == "c")
            {
                _builder.StoreC(_tempC);
                _builder.LoadA(_tempC);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt <= InstructionReference.MaxDataLength)
                {
                    _builder.SetA(valueInt);
                }
                else
                {
                    _builder.SetA(_tempB);
                    _builder.StoreB_ToAddressUsingA();
                    _builder.SetA(valueInt / InstructionReference.MaxDataLength);
                    _builder.SetB(InstructionReference.MaxDataLength);
                    _builder.Mult();

                    var reminder = valueInt % InstructionReference.MaxDataLength;

                    if (reminder > 0)
                    {
                        _builder.SetA(valueInt % InstructionReference.MaxDataLength);
                        _builder.Add();
                    }

                    _builder.LoadB(_tempB);
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
                _builder.StoreA(_tempA);
                _builder.LoadB(_tempA);
            }
            else if (value == "b")
            {
                _builder.Nop();
            }
            else if (value == "c")
            {
                _builder.StoreC(_tempA);
                _builder.LoadB(_tempA);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt < InstructionReference.MaxDataLength)
                {
                    _builder.SetB(valueInt);
                }
                else
                {
                    _builder.StoreA(_tempA);
                    _builder.SetA(valueInt / InstructionReference.MaxDataLength);
                    _builder.SetB(InstructionReference.MaxDataLength);
                    _builder.Mult();

                    var reminder = valueInt % InstructionReference.MaxDataLength;

                    if (reminder > 0)
                    {
                        _builder.SetA(valueInt % InstructionReference.MaxDataLength);
                        _builder.Add();
                    }

                    _builder.StoreA(_tempB);
                    _builder.LoadA(_tempA);
                    _builder.LoadB(_tempB);
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
                _builder.StoreA(_tempA);
                _builder.LoadC(_tempA);
            }
            else if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                _builder.SwapA_B();
                _builder.StoreA(_tempA);
                _builder.SwapA_B();
                _builder.LoadC(_tempA);
            }
            else if (value == "b")
            {
                _builder.Nop();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public int[] ToArray() => _builder.ToArray();
}
