using Astro8.Devices;
using Astro8.Instructions;

namespace Astro8;

public class CpuBuilder<THandler>
    where THandler : Handler
{
    private readonly Config _config;
    private readonly THandler _handler;
    private Screen<THandler>? _screen;
    private CharacterDevice<THandler>? _character;
    private readonly CpuMemory<THandler>?[] _memory = new CpuMemory<THandler>?[2]; // TODO: Make the amount of banks variable

    public CpuBuilder(THandler handler, Config config)
    {
        _handler = handler;
        _config = config;
    }

    public CpuBuilder<THandler> WithMemory(int? size = null)
    {
        for (var i = 0; i < _memory.Length; i++)
        {
            _memory[i] = new CpuMemory<THandler>(i, size ?? _config.Memory.Size);
        }

        return this;
    }

    public CpuBuilder<THandler> WithProgram(IProgram builder)
    {
        var memory = _memory[0];

        if (memory == null)
        {
            throw new InvalidOperationException("Memory must be configured before program");
        }

        builder.CopyTo(memory.Data);
        memory.UpdateInstructions();

        return this;
    }

    public CpuBuilder<THandler> WithProgramFile(string? file = null, int? address = null)
    {
        var memory = _memory[0];

        if (memory == null)
        {
            throw new InvalidOperationException("Memory must be configured before program file");
        }

        HexFile.LoadFile(
            file ?? _config.Program.Path,
            memory.Data,
            address ?? _config.Memory.Devices.Program
        );
        memory.UpdateInstructions();

        return this;
    }

    public CpuBuilder<THandler> WithMemory(int bank, int[] memory)
    {
        _memory[bank] = new CpuMemory<THandler>(bank, memory);
        return this;
    }

    public CpuBuilder<THandler> WithScreen(int bank = 1, int? address = null)
    {
        _screen = new Screen<THandler>(
            bank,
            address ?? _config.Memory.Devices.Screen,
            _handler,
            _config.Screen.Width,
            _config.Screen.Height
        );
        return this;
    }

    public CpuBuilder<THandler> WithCharacter(int bank = 1, int? address = null, bool writeToConsole = false)
    {
        _character = new CharacterDevice<THandler>(
            bank,
            address ?? _config.Memory.Devices.Character,
            _screen,
            _config.Screen.Width,
            _config.Screen.Height,
            writeToConsole: writeToConsole
        );
        return this;
    }

    public Cpu<THandler> Create()
    {
        for (var i = 0; i < _memory.Length; i++)
        {
            _memory[i] ??= new CpuMemory<THandler>(i, 0xFFFF);
        }

        var cpu = new Cpu<THandler>(_memory!, _handler);

        if (_screen != null)
        {
            _memory[_screen.Bank]!.MapScreen(_screen);
        }

        if (_character != null)
        {
            _memory[_character.Bank]!.MapCharacterScreen(_character);
        }

        return cpu;
    }
}

public static class CpuBuilder
{
    public static CpuBuilder<THandler> Create<THandler>(THandler handler, Config? config = null)
        where THandler : Handler
    {
        return new CpuBuilder<THandler>(handler, config ?? new Config());
    }

    public static CpuBuilder<THandler> Create<THandler>(Config? config = null)
        where THandler : Handler, new()
    {
        return Create(new THandler(), config);
    }
}
