using Yabal.Instructions;

namespace Yabal.Devices;

public sealed class CpuMemory<THandler>
    where THandler : Handler
{
    private Screen<THandler>? _screen;
    private int _screenStart;
    private int _screenEnd;

    private CharacterDevice<THandler>? _characterDevice;
    private int _characterDeviceStart;
    private int _characterDeviceEnd;

    private DebugDevice<THandler>? _debugDevice;
    private int _debugDeviceStart;
    private int _debugDeviceEnd;

    public CpuMemory(int bank, int[] data)
    {
        Data = data;
        Bank = bank;
        Instruction = new InstructionReference[data.Length];

        UpdateInstructions();
    }

    public CpuMemory(int bank, int size)
        : this(bank, new int[size])
    {
    }

    public void UpdateInstructions()
    {
        if (Bank != 0)
        {
            return;
        }

        var data = Data.AsSpan();
        var instructions = Instruction.AsSpan();

        for (var i = 0; i < Instruction.Length; i++)
        {
            instructions[i] = new InstructionReference(data[i]);
        }
    }

    public int Bank { get; }

    public int[] Data { get; }

    public InstructionReference[] Instruction { get; }

    public void MapScreen(Screen<THandler> screen)
    {
        var address = screen.Address;
        _screenStart = address;
        _screenEnd = address + screen.Length;
        _screen = screen;
        screen.Initialize(Data.AsSpan(address, screen.Length), false);
    }

    public void MapCharacterScreen(CharacterDevice<THandler> characterDevice)
    {
        var address = characterDevice.Address;
        _characterDeviceStart = address;
        _characterDeviceEnd = address + characterDevice.Length;
        _characterDevice = characterDevice;
        characterDevice.Initialize(Data.AsSpan(address, characterDevice.Length), false);
    }

    public void MapDebug(DebugDevice<THandler> debugDevice)
    {
        var address = debugDevice.Address;
        _debugDeviceStart = address;
        _debugDeviceEnd = address + debugDevice.Length;
        _debugDevice = debugDevice;
        debugDevice.Initialize(Data.AsSpan(address, debugDevice.Length), false);
    }

    public int this[int address]
    {
        get => Data[address];
        set
        {
            if (address < 0 || address >= Data.Length)
            {
                return;
            }

            Data[address] = value;
            OnChange(address, value);

            if (address < Instruction.Length)
            {
                Instruction[address] = new InstructionReference(value);
            }
        }
    }

    public void OnChange(int address, int value)
    {
        if (address >= _screenStart && address < _screenEnd)
        {
            _screen?.Write(address - _screenStart, value);
        }
        else if (address >= _characterDeviceStart && address < _characterDeviceEnd)
        {
            _characterDevice?.Write(address - _characterDeviceStart, value);
        }
        else if (address >= _debugDeviceStart && address < _debugDeviceEnd)
        {
            _debugDevice?.Write(address - _debugDeviceStart, value);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(Data.Length);

        foreach (var value in Data)
        {
            writer.Write(value);
        }
    }

    public void Load(BinaryReader reader)
    {
        var length = reader.ReadInt32();

        if (length != Data.Length)
        {
            throw new Exception($"Memory length mismatch: {length} != {Data.Length}");
        }

        for (var i = 0; i < length; i++)
        {
            var value = reader.ReadInt32();
            Data[i] = value;
            OnChange(i, value);
        }
    }
}
