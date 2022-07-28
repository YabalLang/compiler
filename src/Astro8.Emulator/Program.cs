using Astro8;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;


using var screen = new Screen();

if (!screen.Init())
{
    Console.WriteLine("Failed to initialize SDL2.");
    return 1;
}

var characterScreen = new CharacterDevice(screen);

var instructions = HexFile.LoadFile(@"C:\Temp\Astro8\Win_x64\program_machine_code")
    .Take(0x3FFE)
    .ToArray();

var program = new ArrayDevice(instructions);

var memory = new Memory(0xFFFF);
memory.Map(0x0000, program);
memory.Map(0x3FFE, characterScreen);
memory.Map(0xEFFF, screen);

var cpu = new Cpu(memory);

while (true)
{
    if (!cpu.Step())
    {
        break;
    }

    screen.Update();

    if (SDL_PollEvent(out var e) != 1)
    {
        Thread.Sleep(1);
        continue;
    }

    if (e.type is SDL_APP_TERMINATING or SDL_QUIT)
    {
        break;
    }

    if (e.type is SDL_KEYDOWN)
    {
        cpu.ExpansionPort = Keyboard.ConvertAsciiToSdcii((int) e.key.keysym.scancode);
    }
}

return 0;
