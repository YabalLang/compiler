using System.Diagnostics.CodeAnalysis;

namespace Astro8;

public class Memory
{
    public record MemoryDevice(int Start, int Length, IMemoryDevice Device, Memory Memory)
    {
        public void Initialize()
        {
            var span = Memory._data.AsSpan(Start, Length);
            Device.Initialize(Memory, span);
        }

        public void Write(int address, int value)
        {
            if (address < 0 || address >= Length)
            {
                return;
            }

            Memory._data[Start + address] = value;
            Device.Write(Memory, address, value);
        }

        public int Read(int address)
        {
            if (address < 0 || address >= Length)
            {
                return 0;
            }

            return Memory._data[Start + address];
        }
    }

    private readonly int[] _data;
    private readonly MemoryDevice?[] _deviceMapping;
    private readonly List<MemoryDevice> _devices = new();

    public Memory(int size)
    {
        _data = new int[size];
        _deviceMapping = new MemoryDevice?[size];
    }

    public int Length => _data.Length;

    public bool TryFindMapping(IMemoryDevice device, [NotNullWhen(true)] out MemoryDevice? mapping)
    {
        foreach (var memoryDevice in _devices)
        {
            if (memoryDevice.Device == device)
            {
                mapping = memoryDevice;
                return true;
            }
        }

        mapping = default;
        return false;
    }

    public int Map(int offset, IMemoryDevice device)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        var end = offset + device.Length;

        if (end > _data.Length)
        {
            throw new ArgumentException($"Device extends beyond end of memory: {offset} + {device.Length} > {_data.Length}", nameof(offset));
        }

        var mapping = new MemoryDevice(offset, device.Length, device, this);

        for (var i = offset; i < end; i++)
        {
            if (_deviceMapping[i] is not null)
            {
                throw new Exception($"Device at {i} already registered");
            }

            _deviceMapping[i] = mapping;
        }

        _devices.Add(mapping);
        device.Initialize(this, _data.AsSpan(offset, device.Length));

        return end;
    }

    public int this[int address]
    {
        get => _data[address];
        set
        {
            if (address < 0 || address >= _data.Length)
            {
                return;
            }

            _data[address] = value;

            if (_deviceMapping[address] is { } mapping)
            {
                var offset = address - mapping.Start;
                mapping.Device.Write(this, offset, value);
            }
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(_data.Length);

        foreach (var value in _data)
        {
            writer.Write(value);
        }
    }

    public void Load(BinaryReader reader)
    {
        var length = reader.ReadInt32();

        if (length != _data.Length)
        {
            throw new Exception($"Memory length mismatch: {length} != {_data.Length}");
        }

        for (var i = 0; i < length; i++)
        {
            _data[i] = reader.ReadInt32();
        }

        foreach (var device in _devices)
        {
            device.Initialize();
        }
    }
}
