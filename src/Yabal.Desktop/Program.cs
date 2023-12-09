using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO.Compression;
using static SDL2.SDL;
using static SDL2.SDL.SDL_EventType;
using CliWrap;
using Yabal;
using Yabal.Ast;
using Yabal.Devices;
using Yabal.Loaders;
using Yabal.Utils;
using Zio.FileSystems;
using Command = System.CommandLine.Command;

var filePathOption = new Argument<FilePath>(
    name: "path",
    description: "File to compile.",
    parse: result =>
    {
        if (result.Tokens.Count == 0)
        {
            return default;
        }

        var filePath = Path.GetFullPath(string.Join(" ", result.Tokens.Select(i => i.Value)));

        // Check for ' -' and if the last char is a '.', and not a '/' or '\'
        var startIndex = 0;
        var rest = "";

        while (true)
        {
            var index = filePath.IndexOf(" -", startIndex, StringComparison.Ordinal);

            if (index == -1)
            {
                break;
            }

            var isValid = false;

            for (var i = index; i >= 0; i--)
            {
                if (filePath[i] is '/' or '\\')
                {
                    break;
                }

                if (filePath[i] == '.')
                {
                    isValid = true;
                    break;
                }
            }

            if (!isValid)
            {
                startIndex = index + 1;
                continue;
            }

            rest = filePath.AsSpan(index).Trim().ToString();
            filePath = filePath.Substring(0, index);
            break;
        }

        if (!File.Exists(filePath))
        {
            result.ErrorMessage = $"File '{filePath}' does not exist";
            return default;
        }

        return new FilePath(new FileInfo(filePath), rest);
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

var output = new Option<bool>(
    name: "--out",
    description: "Print the assembly to the console.");

output.AddAlias("-o");

var debugOption = new Option<bool>(
    name: "--debug",
    description: "Print variable allocation to the console.");

debugOption.AddAlias("-d");

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
    nativeOption,
    output,
    debugOption
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
    formatOption,
    debugOption
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

    if (path.Info == null)
    {
        return;
    }

    var outPath = ctx.ParseResult.GetValueForOption(outOption);
    var formats = ctx.ParseResult.GetValueForOption(formatOption) ?? new List<OutputFormat>();
    var debug = ctx.ParseResult.GetValueForOption(debugOption);

    if (formats.Count == 0)
    {
        formats.Add(OutputFormat.Assembly);
    }

    await BuildOutput(path.Info, outPath, formats, debug);
}

async Task BuildOutput(FileSystemInfo path, string? outPath, List<OutputFormat> formats, bool debug)
{
    var code = File.ReadAllText(path.FullName);
    var fs = new PhysicalFileSystem();
    var uri = new Uri("file:///" + fs.ConvertPathFromInternal(path.FullName));
    using var context = new YabalContext(fs);

    #if INCLUDE_LOADERS
    context.AddFileLoader(FileType.Font, FontLoader.Instance);
    context.AddFileLoader(FileType.Image, ImageLoader.Instance);
    #endif

    var builder = new YabalBuilder(context)
    {
        Debug = debug
    };

    await builder.CompileCodeAsync(code, file: uri);
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

    // Local path
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

    // Environment path
    if (Environment.GetEnvironmentVariable("PATH") is { } pathVariables)
    {
        foreach (var path in pathVariables.Split(Path.PathSeparator))
        {
            paths.Add(Path.Combine(path, "astro8.exe"));
            paths.Add(Path.Combine(path, "astro8"));
        }
    }

    // Default install path
    if (OperatingSystem.IsWindows())
    {
        paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Astro8\\astro8.exe"));
        paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Astro8\\astro8"));
    }

    return paths.FirstOrDefault(File.Exists);
}

async Task Execute(InvocationContext ctx)
{
    var path = ctx.ParseResult.GetValueForArgument(filePathOption);

    if (path.Info == null)
    {
        return;
    }

    var code = File.ReadAllText(path.Info.FullName);
    var fs = new PhysicalFileSystem();
    var uri = new Uri("file:///" + fs.ConvertPathFromInternal(path.Info.FullName));
    using var context = new YabalContext(fs);

    #if INCLUDE_LOADERS
    context.AddFileLoader(FileType.Font, FontLoader.Instance);
    context.AddFileLoader(FileType.Image, ImageLoader.Instance);
    #endif

    var showOutput = ctx.ParseResult.GetValueForOption(output);
    var disableScreen = ctx.ParseResult.GetValueForOption(disableScreenOption);
    var disableCharacters = ctx.ParseResult.GetValueForOption(disableCharactersOption);
    var statePath = ctx.ParseResult.GetValueForOption(stateOption);
    var charactersToConsole = ctx.ParseResult.GetValueForOption(charactersToConsoleOption);
    var native = ctx.ParseResult.GetValueForOption(nativeOption);
    var debug = ctx.ParseResult.GetValueForOption(debugOption);

    var builder = new YabalBuilder(context)
    {
        Debug = debug
    };

    await builder.CompileCodeAsync(code, file: uri);

    if (native)
    {
        var fileName = GetNativePath();

        if (fileName == null)
        {
            ctx.Console.WriteLine("Could not find the native emulator.");
            return;
        }

        await BuildOutput(path.Info, null, new List<OutputFormat> { OutputFormat.Assembly }, debug);
        await RunNativeAsync(ctx.Console, fileName, path);

        return;
    }

    var config = ConfigContext.Load(path.Info.Directory?.FullName);
    using var handler = new DesktopHandler(
        config.Screen.Width,
        config.Screen.Height,
        config.Screen.Scale
    );

    var cpuBuilder = CpuBuilder.Create(handler, config);

    cpuBuilder.WithMemory();
    cpuBuilder.WithKeyboard();
    cpuBuilder.WithMouse();

    if (debug)
    {
        cpuBuilder.WithDebug();
    }

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

    if (showOutput)
    {
        Console.WriteLine(program.ToAssembly(true));
    }

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

async Task RunNativeAsync(IConsole console, string fileName, FilePath fileInfo, bool chmod = true)
{
    try
    {
        (int Left, int Top)? lastFreq = null;

        await Cli.Wrap(fileName)
            .WithArguments(new []
            {
                Path.ChangeExtension(fileInfo.Info!.FullName, ".asm")
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(s =>
            {
                if (string.IsNullOrWhiteSpace(s))
                {
                    return;
                }

                if (s.Contains("FPS:", StringComparison.OrdinalIgnoreCase))
                {
                    if (lastFreq is { } freq)
                    {
                        Console.SetCursorPosition(freq.Left, freq.Top);
                    }
                    else
                    {
                        lastFreq = Console.GetCursorPosition();
                    }
                }
                else
                {
                    lastFreq = null;
                }

                console.WriteLine(s);
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(console.WriteLine))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }
    catch (Win32Exception) when (OperatingSystem.IsLinux() && chmod)
    {
        // Try to chmod +x and run again
        await Cli.Wrap("chmod")
            .WithArguments(new[] { "+x", fileName })
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        await RunNativeAsync(console, fileName, fileInfo, false);
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

internal record struct FilePath(FileInfo? Info, string Arguments);