namespace Astro8;

public class Memory
{
    public record MemoryDevice(int Start, int Length, IMemoryDevice Device);

    private readonly int[] _data;
    private readonly MemoryDevice?[] _devices;

    public Memory(int size)
    {
        _data = new int[size];
        _devices = new MemoryDevice?[size];
    }

    public int Length => _data.Length;

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

        var mapping = new MemoryDevice(offset, device.Length, device);

        for (var i = offset; i < end; i++)
        {
            if (_devices[i] is not null)
            {
                throw new Exception($"Device at {i} already registered");
            }

            _devices[i] = mapping;
        }

        return end;
    }

    public int this[int address]
    {
        get
        {
            if (_devices[address] is {} mapping)
            {
                return mapping.Device.Read(address - mapping.Start);
            }

            return _data[address];
        }
        set
        {
            if (_devices[address] is {} mapping)
            {
                mapping.Device.Write(address - mapping.Start, value);
            }
            else
            {
                _data[address] = value;
            }
        }
    }
}
