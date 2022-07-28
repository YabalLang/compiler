using Astro8;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;

var config = Config.Load();

using var screen = new Screen(
    config.Screen.Width,
    config.Screen.Height,
    config.Screen.Scale
);

if (!screen.Init())
{
    Console.WriteLine("Failed to initialize SDL2.");
    return 1;
}

var characterScreen = new CharacterDevice(screen);

var instructions = HexFile.LoadFile(config.Program.Path)
    .Take(0x3FFE)
    .ToArray();

var program = new ArrayDevice(instructions);

var memory = new Memory(config.Memory.Size);

if (config.Memory.Devices is null)
{
    memory.Map(0x0000, program);
    memory.Map(0x3FFE, characterScreen);
    memory.Map(0xEFFF, screen);
}
else
{
    foreach (var deviceConfig in config.Memory.Devices)
    {
        IMemoryDevice device = deviceConfig.Type.ToLowerInvariant() switch
        {
            "program" => program,
            "character" => characterScreen,
            "screen" => screen,
            _ => throw new Exception($"Unknown device type: {deviceConfig.Type}")
        };

        memory.Map(deviceConfig.Address, device);
    }
}

var cpu = new Cpu(memory);
cpu.RunThread(config.Cpu.TickSpeed);

while (cpu.Running)
{
    screen.Update();

    if (SDL_PollEvent(out var e) != 1)
    {
        continue;
    }

    if (e.type is SDL_APP_TERMINATING or SDL_QUIT)
    {
        cpu.Halt();
        break;
    }

    if (e.type is SDL_KEYDOWN)
    {
        switch (e.key.keysym.scancode)
        {
            case SDL_Scancode.SDL_SCANCODE_F11:
            {
                using var file = File.Open("state", FileMode.Create);
                cpu.Save(file);
                break;
            }
            case SDL_Scancode.SDL_SCANCODE_F12:
            {
                if (!File.Exists("state"))
                {
                    // TODO: Show error message.
                    continue;
                }

                using var file = File.Open("state", FileMode.Open);
                cpu.Load(file);
                break;
            }
            default:
                cpu.ExpansionPort = Keyboard.ConvertAsciiToSdcii((int) e.key.keysym.scancode);
                break;
        }
    }
}

return 0;
