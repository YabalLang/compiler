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
    private CpuMemory<THandler>? _memory;
    private MicroInstruction[]? _microInstructions;

    public CpuBuilder(THandler handler, Config config)
    {
        _handler = handler;
        _config = config;
    }

    public CpuBuilder<THandler> WithInstructions(MicroInstruction[] microInstructions)
    {
        _microInstructions = microInstructions;
        return this;
    }

    public CpuBuilder<THandler> WithMemory(int? size = null)
    {
        _memory = new CpuMemory<THandler>(size ?? _config.Memory.Size);
        return this;
    }

    public CpuBuilder<THandler> WithProgram(InstructionBuilder builder)
    {
        if (_memory == null)
        {
            throw new InvalidOperationException("Memory must be configured before program");
        }

        builder.ToArray(_memory.Data);

        return this;
    }

    public CpuBuilder<THandler> WithProgramFile(string? file = null, int? address = null)
    {
        if (_memory == null)
        {
            throw new InvalidOperationException("Memory must be configured before program file");
        }

        HexFile.LoadFile(
            file ?? _config.Program.Path,
            _memory.Data,
            address ?? _config.Memory.Devices.Program
        );

        return this;
    }

    public CpuBuilder<THandler> WithMemory(int[] memory)
    {
        _memory = new CpuMemory<THandler>(memory);
        return this;
    }

    public CpuBuilder<THandler> WithScreen(int? address = null)
    {
        _screen = new Screen<THandler>(address ?? _config.Memory.Devices.Screen, _handler);
        return this;
    }

    public CpuBuilder<THandler> WithCharacter(int? address = null)
    {
        if (_screen == null)
        {
            throw new InvalidOperationException("Screen must be configured before character");
        }

        _character = new CharacterDevice<THandler>(address ?? _config.Memory.Devices.Character, _screen);
        return this;
    }

    public Cpu<THandler> Create()
    {
        var memory = _memory ?? new CpuMemory<THandler>();
        var instructions = _microInstructions ?? MicroInstruction.Default;
        var cpu = new Cpu<THandler>(memory, instructions);

        if (_screen != null)
        {
            memory.MapScreen(_screen);
        }

        if (_character != null)
        {
            memory.MapCharacterScreen(_character);
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
