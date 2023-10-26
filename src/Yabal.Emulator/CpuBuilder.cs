using Yabal;
using Yabal.Devices;

namespace Yabal;

public class CpuBuilder<THandler>
    where THandler : Handler
{
    private readonly Config _config;
    private readonly THandler _handler;
    private Screen<THandler>? _screen;
    private CharacterDevice<THandler>? _character;
    private DebugDevice<THandler>? _debug;
    private Address? _keyboardAddress;
    private Address? _mouseAddress;
    private readonly CpuMemory<THandler>?[] _memory;

    public CpuBuilder(THandler handler, Config config)
    {
        _handler = handler;
        _config = config;
        _memory = new CpuMemory<THandler>?[config.Memory.Banks];
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

    public CpuBuilder<THandler> WithProgramFile(string? file = null, Address? address = null)
    {
        var (bank, offset) = address ?? _config.Memory.Devices.Program;
        var memory = _memory[bank];

        if (memory == null)
        {
            throw new InvalidOperationException("Memory must be configured before program file");
        }

        HexFile.LoadFile(
            file ?? _config.Program.Path,
            memory.Data,
            offset
        );
        memory.UpdateInstructions();

        return this;
    }

    public CpuBuilder<THandler> WithMemory(int bank, int[] memory)
    {
        _memory[bank] = new CpuMemory<THandler>(bank, memory);
        return this;
    }

    public CpuBuilder<THandler> WithScreen(Address? address = null)
    {
        var (bank, offset) = address ?? _config.Memory.Devices.Screen;

        _screen = new Screen<THandler>(
            bank,
            offset,
            _handler,
            _config.Screen.Width,
            _config.Screen.Height
        );
        return this;
    }

    public CpuBuilder<THandler> WithCharacter(Address? address = null, bool writeToConsole = false)
    {
        var (bank, offset) = address ?? _config.Memory.Devices.Character;

        _character = new CharacterDevice<THandler>(
            bank,
            offset,
            _screen,
            _config.Screen.Width,
            _config.Screen.Height,
            writeToConsole: writeToConsole
        );
        return this;
    }

    public CpuBuilder<THandler> WithKeyboard(Address? address = null)
    {
        _keyboardAddress = address ?? _config.Memory.Devices.Keyboard;
        return this;
    }

    public CpuBuilder<THandler> WithMouse(Address? address = null)
    {
        _mouseAddress = address ?? _config.Memory.Devices.Mouse;
        return this;
    }

    public CpuBuilder<THandler> WithDebug(Address? address = null)
    {
        var (bank, offset) = address ?? new Address(Bank: 5, Offset: 0);

        _debug = new DebugDevice<THandler>(bank, offset, _handler);
        return this;
    }

    public Cpu<THandler> Create()
    {
        for (var i = 0; i < _memory.Length; i++)
        {
            _memory[i] ??= new CpuMemory<THandler>(i, 0xFFFF);
        }

        var cpu = new Cpu<THandler>(_memory!, _handler, _keyboardAddress, _mouseAddress);

        if (_screen != null)
        {
            _memory[_screen.Bank]!.MapScreen(_screen);
        }

        if (_character != null)
        {
            _memory[_character.Bank]!.MapCharacterScreen(_character);
        }

        if (_debug != null)
        {
            _memory[_debug.Bank]!.MapDebug(_debug);
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
