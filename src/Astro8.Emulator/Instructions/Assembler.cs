namespace Astro8.Instructions;

public class Assembler
{
    private readonly InstructionPointer _tempA;
    private readonly InstructionPointer _tempB;
    private readonly InstructionPointer _tempC;

    public Assembler()
    {
        InstructionBuilder = new InstructionBuilder();

        var label = InstructionBuilder.CreateLabel();
        InstructionBuilder.Jump(label);
        _tempA = InstructionBuilder.Nop().CreatePointer();
        _tempB = InstructionBuilder.Nop().CreatePointer();
        _tempC = InstructionBuilder.Nop().CreatePointer();
        label.Mark();
    }

    public InstructionBuilder InstructionBuilder { get; }

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
                InstructionBuilder.StoreC(_tempC);
                InstructionBuilder.SwapA_B();
                InstructionBuilder.StoreA(_tempA);
                InstructionBuilder.LoadB(_tempA);
                InstructionBuilder.LoadC(_tempC);
            }
            else if (value == "c")
            {
                InstructionBuilder.StoreC(_tempC);
                InstructionBuilder.LoadA(_tempC);
            }
            else if (int.TryParse(value, out var valueInt))
            {
                if (valueInt <= InstructionReference.MaxDataLength)
                {
                    InstructionBuilder.SetA(valueInt);
                }
                else
                {
                    InstructionBuilder.SetA(_tempB);
                    InstructionBuilder.StoreB_ToAddressUsingA();
                    InstructionBuilder.SetA(valueInt / InstructionReference.MaxDataLength);
                    InstructionBuilder.SetB(InstructionReference.MaxDataLength);
                    InstructionBuilder.Mult();

                    var reminder = valueInt % InstructionReference.MaxDataLength;

                    if (reminder > 0)
                    {
                        InstructionBuilder.SetA(valueInt % InstructionReference.MaxDataLength);
                        InstructionBuilder.Add();
                    }

                    InstructionBuilder.LoadB(_tempB);
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
                    InstructionBuilder.StoreA(_tempA);
                    InstructionBuilder.SetA(valueInt / InstructionReference.MaxDataLength);
                    InstructionBuilder.SetB(InstructionReference.MaxDataLength);
                    InstructionBuilder.Mult();

                    var reminder = valueInt % InstructionReference.MaxDataLength;

                    if (reminder > 0)
                    {
                        InstructionBuilder.SetA(valueInt % InstructionReference.MaxDataLength);
                        InstructionBuilder.Add();
                    }

                    InstructionBuilder.StoreA(_tempB);
                    InstructionBuilder.LoadA(_tempA);
                    InstructionBuilder.LoadB(_tempB);
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
            else
            {
                throw new NotImplementedException();
            }
        }
    }

    public int[] ToArray() => InstructionBuilder.ToArray();
}
