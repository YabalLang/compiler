using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.IO.Compression;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;
using CliWrap;
using Yabal;
using Yabal.Devices;
using Yabal.Utils;
using Zio.FileSystems;
using Command = System.CommandLine.Command;

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

var nativeOption = new Option<bool>(
    name: "--native",
    description: "Use the official emulator.");

nativeOption.AddAlias("-n");

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
    stateOption,
    nativeOption
};

run.SetHandler(Execute);

var outOption = new Option<string>(
    name: "--out",
    description: "Output path of the assembly.");

outOption.AddAlias("-o");

var formatOption = new Option<List<OutputFormat>?>(
    name: "--format",
    description: "Change the output format.",
    parseArgument: EnumHelper.ParseEnum<OutputFormat>);

formatOption.AddAlias("-f");

var build = new Command(
    name: "build",
    description: "Compiles the given file to an assembly file")
{
    filePathOption,
    outOption,
    formatOption
};

build.SetHandler(Build);

var rootCommand = new RootCommand
{
    run,
    build
};

async Task Build(InvocationContext ctx)
{
    var path = ctx.ParseResult.GetValueForArgument(filePathOption);

    if (path == null)
    {
        return;
    }

    var outPath = ctx.ParseResult.GetValueForOption(outOption);
    var formats = ctx.ParseResult.GetValueForOption(formatOption) ?? new List<OutputFormat>();

    if (formats.Count == 0)
    {
        formats.Add(OutputFormat.Assembly);
    }

    await BuildOutput(path, outPath, formats);
}

async Task BuildOutput(FileSystemInfo path, string? outPath, List<OutputFormat> formats)
{
    var builder = new YabalBuilder();
    var code = File.ReadAllText(path.FullName);
    var fs = new PhysicalFileSystem();
    var uri = new Uri("file:///" + fs.ConvertPathFromInternal(path.FullName));
    await builder.CompileCodeAsync(code, fileSystem: fs, file: uri);
    PrintErrors(uri, builder.Errors, code);

    if (string.IsNullOrEmpty(outPath))
    {
        outPath = path.FullName;
    }
    else if (Path.EndsInDirectorySeparator(outPath) || Directory.Exists(outPath))
    {
        outPath = Path.Combine(outPath, Path.GetFileName(path.FullName));
    }

    foreach (var format in formats)
    {
        var filePath = Path.ChangeExtension(outPath, format switch
        {
            OutputFormat.Assembly => ".asm",
            OutputFormat.AstroExecutable => ".aexe",
            OutputFormat.Logisim => ".hex",
            OutputFormat.AssemblyWithComments => ".asmc",
            _ => throw new NotSupportedException()
        });

        await using var file = File.Open(filePath, FileMode.Create);
        await using var writer = new StreamWriter(file);

        switch (format)
        {
            case OutputFormat.Assembly:
                builder.ToAssembly(writer);
                Console.WriteLine($"Assembly file written to {filePath}");
                break;
            case OutputFormat.AssemblyWithComments:
                builder.ToAssembly(writer, true);
                Console.WriteLine($"Assembly with comments file written to {filePath}");
                break;
            case OutputFormat.AstroExecutable:
                builder.ToHex(writer);
                Console.WriteLine($"Astro Executable written to {filePath}");
                break;
            case OutputFormat.Logisim:
                builder.ToLogisimFile(writer, 0xFFFF);
                Console.WriteLine($"Logisim Evolution file written to {filePath}");
                break;
            default:
                throw new NotSupportedException();
        }
    }
}

string? GetNativePath()
{
    var paths = new List<string>
    {
        "astro8.exe",
        "astro8"
    };

    if (Path.GetDirectoryName(typeof(Program).Assembly.Location) is { } location)
    {
        paths.Add(Path.Combine(location, "astro8.exe"));
        paths.Add(Path.Combine(location, "astro8"));

        var nativeFolder = Path.Combine(location, "native");

        if (Directory.Exists(nativeFolder))
        {
            paths.Add(Path.Combine(nativeFolder, "astro8.exe"));
            paths.Add(Path.Combine(nativeFolder, "astro8"));
        }
    }

    return paths.FirstOrDefault(File.Exists);
}

async Task Execute(InvocationContext ctx)
{
    var path = ctx.ParseResult.GetValueForArgument(filePathOption);

    if (path == null)
    {
        return;
    }

    var builder = new YabalBuilder();
    var code = File.ReadAllText(path.FullName);
    var fs = new PhysicalFileSystem();
    var uri = new Uri("file:///" + fs.ConvertPathFromInternal(path.FullName));
    await builder.CompileCodeAsync(code, fileSystem: fs, file: uri);

    var disableScreen = ctx.ParseResult.GetValueForOption(disableScreenOption);
    var disableCharacters = ctx.ParseResult.GetValueForOption(disableCharactersOption);
    var statePath = ctx.ParseResult.GetValueForOption(stateOption);
    var charactersToConsole = ctx.ParseResult.GetValueForOption(charactersToConsoleOption);
    var native = ctx.ParseResult.GetValueForOption(nativeOption);

    if (native)
    {
        var fileName = GetNativePath();

        if (fileName == null)
        {
            ctx.Console.WriteLine("Could not find the native emulator.");
            return;
        }

        await BuildOutput(path, null, new List<OutputFormat> { OutputFormat.AssemblyWithComments });

        await Cli.Wrap(fileName)
            .WithArguments(new[]
            {
                Path.ChangeExtension(path.FullName, ".asmc"),
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s => ctx.Console.WriteLine(s)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(s => ctx.Console.WriteLine(s)))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        return;
    }

    var config = ConfigContext.Load(path.Directory?.FullName);
    using var handler = new DesktopHandler(
        config.Screen.Width,
        config.Screen.Height,
        config.Screen.Scale
    );

    var cpuBuilder = CpuBuilder.Create(handler, config);

    cpuBuilder.WithMemory();
    cpuBuilder.WithKeyboard();
    cpuBuilder.WithMouse();

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

    var program = builder.Build();
    PrintErrors(uri, builder.Errors, code);

    cpuBuilder.WithProgram(program);

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
                            cpu.SetKeyboard(value);
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

            switch (e.type)
            {
                case SDL_KEYDOWN:
                {
                    switch (e.key.keysym.scancode)
                    {
                        case SDL_Scancode.SDL_SCANCODE_F11 when statePath != null:
                            SaveState();
                            break;
                        case SDL_Scancode.SDL_SCANCODE_F12 when statePath != null:
                            LoadState();
                            break;
                    }

                    cpu.SetKeyboard(
                        Keyboard.Table.TryGetValue((int) e.key.keysym.scancode, out var keyCode)
                            ? keyCode
                            : 168
                    );

                    break;
                }
                case SDL_KEYUP:
                {
                    cpu.SetKeyboard(168);
                    break;
                }
                case SDL_MOUSEMOTION:
                    cpu.SetMousePosition(e.motion.x, e.motion.y);
                    break;
                case SDL_MOUSEBUTTONDOWN when e.button.button == SDL_BUTTON_LEFT:
                    cpu.SetMouseButton(MouseButton.Left, true);
                    break;
                case SDL_MOUSEBUTTONUP when e.button.button == SDL_BUTTON_LEFT:
                    cpu.SetMouseButton(MouseButton.Left, false);
                    break;
                case SDL_MOUSEBUTTONDOWN when e.button.button == SDL_BUTTON_RIGHT:
                    cpu.SetMouseButton(MouseButton.Right, true);
                    break;
                case SDL_MOUSEBUTTONUP when e.button.button == SDL_BUTTON_RIGHT:
                    cpu.SetMouseButton(MouseButton.Right, false);
                    break;
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

void PrintErrors(Uri uri, IReadOnlyDictionary<SourceRange, List<CompileError>> errorByRange, string code)
{
    if (errorByRange.Count == 0)
    {
        return;
    }

    foreach (var (range, errors) in errorByRange.OrderBy(i => i.Key))
    {
        Console.Write($"[{range.File}:{range.StartLine}]");

        if (errors.Count == 1)
        {
            Console.Write(" ");
            WriteError(errors[0]);
        }
        else
        {
            Console.WriteLine();
            foreach (var error in errors)
            {
                Console.Write(" - ");
                WriteError(error);
            }
        }

        if (range.File == uri)
        {
            Console.WriteLine(code.GetPeek(range));
            Console.WriteLine();
        }
    }
}

void WriteError(CompileError compileError)
{
    var color = Console.ForegroundColor;

    Console.ForegroundColor = compileError.Level switch
    {
        ErrorLevel.Error => ConsoleColor.DarkRed,
        ErrorLevel.Warning => ConsoleColor.DarkYellow,
        ErrorLevel.Debug => ConsoleColor.DarkGray,
        _ => color
    };

    Console.Write(compileError.Level);

    Console.ForegroundColor = color;

    Console.Write(": ");
    Console.WriteLine(compileError.Message);
}
