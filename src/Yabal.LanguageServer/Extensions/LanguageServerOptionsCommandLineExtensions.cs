using System;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Yabal.LanguageServer;

static class LanguageServerOptionsCommandLineExtensions
{
    public static LanguageServerOptions WithCommandLineCommunicationChannel(this LanguageServerOptions options,
        string[] args)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        CommandLineOptions commandLineOptions = CommandLineOptions.Parse(args);

        NetworkStream? stream;

        switch (commandLineOptions.CommunicationChannel)
        {
            case CommunicationChannel.ConsoleInputOutput:
                options.WithInput(Console.OpenStandardInput());
                options.WithOutput(Console.OpenStandardOutput());
                break;
            case CommunicationChannel.Pipe:
                if (OperatingSystem.IsWindows())
                {
                    NamedPipeClientStream pipe = new NamedPipeClientStream(".", commandLineOptions.PipeName,
                        PipeDirection.InOut, PipeOptions.Asynchronous);
                    pipe.Connect();
                    options.WithInput(pipe);
                    options.WithOutput(pipe);
                }
                else
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    var endpoint = new UnixDomainSocketEndPoint(commandLineOptions.PipeName);
                    socket.Connect(endpoint);
                    stream = new NetworkStream(socket);

                    options.WithInput(stream);
                    options.WithOutput(stream);
                }

                break;
            default:
                Debug.Assert(commandLineOptions.CommunicationChannel == CommunicationChannel.Socket);
                TcpClient client = new TcpClient();
                options.RegisterForDisposal(client);
                client.Connect(IPAddress.Loopback, commandLineOptions.Port);
                stream = client.GetStream();
                options.WithInput(stream);
                options.WithOutput(stream);
                break;
        }

        return options;
    }

    enum CommunicationChannel
    {
        ConsoleInputOutput,
        Pipe,
        Socket
    }

    struct CommandLineOptions
    {
        public CommunicationChannel CommunicationChannel { get; init; }

        public string PipeName { get; init; }

        public int Port { get; init; }

        private static string ParsePipeNameWindows(string firstArgument, string? secondArgument)
        {
            if (firstArgument == "--pipe" && secondArgument != null && secondArgument.StartsWith(@"\\.\pipe\"))
            {
                return secondArgument.Substring(@"\\.\pipe\".Length);
            }
            else if (firstArgument.StartsWith(@"--pipe=\\.\pipe\") && secondArgument == null)
            {
                return firstArgument.Substring(@"--pipe=\\.\pipe\".Length);
            }

            throw new Exception("Invalid pipe argument");
        }


        private static string ParsePipeNamePosix(string firstArgument, string? secondArgument)
        {
            if (firstArgument == "--pipe" && secondArgument != null)
            {
                return secondArgument;
            }
            else if (firstArgument.StartsWith(@"--pipe=") && secondArgument == null)
            {
                var sockPath = firstArgument.Substring(@"--pipe=".Length);
                return sockPath;
            }

            throw new Exception("Invalid pipe argument");
        }

        private static string ParsePipeName(string firstArgument, string? secondArgument)
        {
            if (OperatingSystem.IsWindows())
            {
                return ParsePipeNameWindows(firstArgument, secondArgument);
            }

            return ParsePipeNamePosix(firstArgument, secondArgument);
        }

        public static CommandLineOptions Parse(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new CommandLineOptions {CommunicationChannel = CommunicationChannel.ConsoleInputOutput};
            }

            if (args.Length <= 2)
            {
                string firstArgument = args[0];
                string? secondArgument = args.Length > 1 ? args[1] : null;

                if (firstArgument == "--stdio" && secondArgument == null)
                {
                    return new CommandLineOptions {CommunicationChannel = CommunicationChannel.ConsoleInputOutput};
                }

                if (firstArgument.StartsWith("--pipe"))
                {
                    return new CommandLineOptions
                    {
                        CommunicationChannel = CommunicationChannel.Pipe,
                        PipeName = ParsePipeName(firstArgument, secondArgument)
                    };
                }
                if (firstArgument.StartsWith("--socket"))
                {
                    if (firstArgument == "--socket" && secondArgument != null)
                    {
                        return new CommandLineOptions
                        {
                            CommunicationChannel = CommunicationChannel.Socket,
                            Port = int.Parse(secondArgument, NumberStyles.None, CultureInfo.InvariantCulture)
                        };
                    }

                    if (firstArgument.StartsWith("--socket=") && secondArgument == null)
                    {
                        int port = int.Parse(firstArgument.Substring("--socket=".Length), NumberStyles.None,  CultureInfo.InvariantCulture);
                        return new CommandLineOptions
                        {
                            CommunicationChannel = CommunicationChannel.Socket,
                            Port = port
                        };
                    }
                }
            }

            throw new ArgumentException("Invalid command line communication channel arguments.", nameof(args));
        }
    }
}
