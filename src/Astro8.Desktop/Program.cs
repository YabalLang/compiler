using Astro8;
using Astro8.Devices;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;

var config = ConfigContext.Load();

using var handler = new DesktopHandler(
    config.Screen.Width,
    config.Screen.Height,
    config.Screen.Scale
);

if (!handler.Init())
{
    Console.WriteLine("Failed to initialize SDL2.");
    return 1;
}

var cpu = CpuBuilder.Create(handler, config)
    .WithScreen()
    .WithCharacter()
    .WithMemory()
    .WithProgramFile()
    .Create();

cpu.RunThread(
    config.Cpu.CycleDuration,
    config.Cpu.InstructionsPerCycle
);

while (cpu.Running)
{
    handler.Update();

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
