namespace Astro8;

public class Assembler
{
    private readonly InstructionBuilder _builder;
    private readonly int _varA;
    private readonly int _varB;
    private readonly int _varC;

    public Assembler()
    {
        _builder = new InstructionBuilder();

        var label = _builder.CreateLabel();
        _builder.Jump(label);
        _varA = _builder.Nop().Count;
        _varB = _builder.Nop().Count;
        _varC = _builder.Nop().Count;
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
            if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                _builder.StoreC(_varC);
                _builder.SwapA_B();
                _builder.StoreA(_varA);
                _builder.LoadB(_varA);
                _builder.LoadC(_varC);
            }
            else if (value == "c")
            {
                _builder.StoreC(_varC);
                _builder.LoadA(_varC);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt <= Instruction.MaxDataLength)
                {
                    _builder.SetA(valueInt);
                }
                else
                {
                    _builder.StoreA(_varB);
                    _builder.SetA(valueInt / Instruction.MaxDataLength);
                    _builder.SetB(Instruction.MaxDataLength);
                    _builder.Mult();
                    _builder.SetA(valueInt % Instruction.MaxDataLength);
                    _builder.Add();
                    _builder.LoadB(_varB);
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
                _builder.StoreA(_varA);
                _builder.LoadB(_varA);
            }
            else if (value == "c")
            {
                _builder.StoreC(_varC);
                _builder.LoadB(_varC);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt < Instruction.MaxDataLength)
                {
                    _builder.SetB(valueInt);
                }
                else
                {

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
                _builder.StoreA(_varA);
                _builder.LoadC(_varA);
            }
            else if (value == "b")
            {
                // You can't store B directly to memory, so we have to swap values around.
                _builder.SwapA_B();
                _builder.StoreA(_varA);
                _builder.SwapA_B();
                _builder.LoadC(_varA);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new Exception("Invalid value");
            }
        }
    }

    public int[] ToArray() => _builder.ToArray();
}
