using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO.Compression;
using Astro8;
using Astro8.Devices;
using Astro8.Instructions;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;

var filePathOption = new Argument<FileInfo?>(
    name: "path",
    description: "File to compile.",
    parse: result =>
    {
        if (result.Tokens.Count == 0)
        {
            return null;
        }

        var filePath = Path.GetFullPath(string.Join(" ", result.Tokens.Select(i => i.Value)));

        if (!File.Exists(filePath))
        {
            result.ErrorMessage = $"File '{filePath}' does not exist";
            return null;
        }

        return new FileInfo(filePath);
    })
{
    Arity = ArgumentArity.OneOrMore
};

var disableScreenOption = new Option<bool>(
    name: "--disable-screen",
    description: "Disable the screen.");

disableScreenOption.AddAlias("-xS");

var disableCharactersOption = new Option<bool>(
    name: "--disable-characters",
    description: "Disable the character set.");

disableCharactersOption.AddAlias("--disable-chars");
disableCharactersOption.AddAlias("-xC");

var charactersToConsoleOption = new Option<bool>(
    name: "--console",
    description: "Redirect character set to the console.");

charactersToConsoleOption.AddAlias("-c");

var stateOption = new Option<string>(
    name: "--state",
    description: "Path of the state.");

charactersToConsoleOption.AddAlias("-s");

var run = new Command(
    name: "run",
    description: "Run the given file")
{
    filePathOption,
    disableScreenOption,
    disableCharactersOption,
    charactersToConsoleOption,
    stateOption
};

run.SetHandler(Execute);

var outOption = new Option<string>(
    name: "--out",
    description: "Output path of the assembly.");

outOption.AddAlias("-o");

var hexOption = new Option<bool>(
    name: "--hex-file",
    description: "Set the output to binary HEX file (program_machine_code).");

hexOption.AddAlias("--hex");
hexOption.AddAlias("-x");

var commentsOption = new Option<bool>(
    name: "--comments",
    description: "Add comments to the assembly (preview).");

commentsOption.AddAlias("-c");

var build = new Command(
    name: "build",
    description: "Compiles the given file to an assembly file")
{
    filePathOption,
    outOption,
    commentsOption,
    hexOption
};

build.SetHandler(Build);

var rootCommand = new RootCommand
{
    run,
    build
};

void Build(InvocationContext ctx)
{
    var path = ctx.ParseResult.GetValueForArgument(filePathOption);

    if (path == null)
    {
        return;
    }

    var outPath = ctx.ParseResult.GetValueForOption(outOption);

    if (string.IsNullOrEmpty(outPath))
    {
        outPath = Path.ChangeExtension(path.FullName, ".asm");
    }

    var comments = ctx.ParseResult.GetValueForOption(commentsOption);
    var hex = ctx.ParseResult.GetValueForOption(hexOption);

    var builder = new YabalBuilder();
    builder.CompileCode(File.ReadAllText(path.FullName));

    using var file = File.Open(outPath, FileMode.Create);
    using var writer = new StreamWriter(file);

    if (hex)
    {
        builder.ToHexFile(writer, 0xFFFF);
    }
    else
    {
        builder.ToAssembly(writer, comments);
    }

    Console.WriteLine($"Assembly file written to {outPath}");
}

void Execute(InvocationContext ctx)
{
    var path = ctx.ParseResult.GetValueForArgument(filePathOption);

    if (path == null)
    {
        return;
    }

    var disableScreen = ctx.ParseResult.GetValueForOption(disableScreenOption);
    var disableCharacters = ctx.ParseResult.GetValueForOption(disableCharactersOption);
    var statePath = ctx.ParseResult.GetValueForOption(stateOption);
    var charactersToConsole = ctx.ParseResult.GetValueForOption(charactersToConsoleOption);

    var config = ConfigContext.Load();
    using var handler = new DesktopHandler(
        config.Screen.Width,
        config.Screen.Height,
        config.Screen.Scale
    );

    var cpuBuilder = CpuBuilder.Create(handler, config);

    cpuBuilder.WithMemory();

    if (!disableScreen)
    {
        cpuBuilder.WithScreen();

        if (!handler.Init())
        {
            ctx.Console.WriteLine("Failed to initialize SDL2.");
            return;
        }
    }

    if (!disableCharacters)
    {
        cpuBuilder.WithCharacter(writeToConsole: charactersToConsole);

        if (charactersToConsole)
        {
            Console.CursorVisible = false;
            Console.Clear();
        }
    }

    var builder = new YabalBuilder();
    builder.CompileCode(File.ReadAllText(path.FullName));
    cpuBuilder.WithProgram(builder);

    var cpu = cpuBuilder.Create();

    if (!string.IsNullOrEmpty(statePath) && File.Exists(statePath))
    {
        LoadState();
    }

    cpu.RunThread(
        config.Cpu.CycleDuration,
        config.Cpu.InstructionsPerCycle
    );

    Console.CancelKeyPress += (_, _) =>
    {
        Console.CursorVisible = true;
    };

    try
    {
        // Console screen
        if (disableScreen)
        {
            while (cpu.Running)
            {
                var key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.F11 when statePath != null:
                        SaveState();
                        break;
                    case ConsoleKey.F12 when statePath != null:
                        LoadState();
                        break;
                    default:
                    {
                        if (Character.CharToInt.TryGetValue(key.KeyChar, out var value))
                        {
                            cpu.ExpansionPort = value;
                        }

                        break;
                    }
                }
            }

            return;
        }

        // SDL2 screen
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
                    case SDL_Scancode.SDL_SCANCODE_F11 when statePath != null:
                    {
                        SaveState();
                        break;
                    }
                    case SDL_Scancode.SDL_SCANCODE_F12 when statePath != null:
                    {
                        LoadState();
                        break;
                    }
                    default:
                        cpu.ExpansionPort = Keyboard.ConvertAsciiToSdcii((int)e.key.keysym.scancode);
                        break;
                }
            }
            else if (e.type is SDL_KEYUP)
            {
                cpu.ExpansionPort = 168;
            }
        }
    }
    finally
    {
        Console.CursorVisible = true;
    }

    void SaveState()
    {
        using var file = File.Open(statePath, FileMode.Create);
        using var stream = new GZipStream(file, CompressionMode.Compress);
        cpu.Save(stream);
    }

    void LoadState()
    {
        if (!File.Exists(statePath))
        {
            return;
        }

        using var file = File.Open(statePath, FileMode.Open);
        using var stream = new GZipStream(file, CompressionMode.Decompress);
        cpu.Load(stream);
    }
}

return await rootCommand.InvokeAsync(args, console: new SystemConsole());
