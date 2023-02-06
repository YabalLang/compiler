using System.Text;
using Yabal.Bot.Handler;
using Yabal.Instructions;
using OneOf;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Rest.Core;
using Remora.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Yabal;
using Yabal.Ast;
using Zio;
using Zio.FileSystems;

namespace Yabal.Bot.Responders;

public class MessageCreateResponder : IResponder<IMessageCreate>
{
    private readonly IDiscordRestChannelAPI _channelApi;

    public MessageCreateResponder(IDiscordRestChannelAPI channelApi)
    {
        _channelApi = channelApi;
    }

    public Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
    {
        const string prefix = "$eval";

        if (!gatewayEvent.Content.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result.FromSuccess());
        }

        var span = gatewayEvent.Content.AsSpan().Slice(prefix.Length).Trim();

        if (span.StartsWith("```"))
        {
            var newLine = span.IndexOf('\n');

            if (newLine != -1)
            {
                span = span[newLine..].Trim();
            }

            if (span.EndsWith("```"))
            {
                span = span[..^3].Trim();
            }
        }

        var code = span.ToString();

        return Execute(code, gatewayEvent.ChannelID);
    }

    private async Task<Result> Execute(string code, Snowflake channelId)
    {
        var builder = new YabalBuilder();
        var fileSystem = new MemoryFileSystem();
        fileSystem.WriteAllText("/message.yabal", code);
        var success = await builder.CompileCodeAsync(code, fileSystem: fileSystem, file: new Uri("file:///message.yabal"));

        var sb = new StringBuilder();
        var handler = new ImageHandler();
        var cpu = CpuBuilder.Create(handler)
            .WithMemory(0xFFFF)
            .WithProgram(builder)
            .WithScreen()
            .WithCharacter()
            .Create();

        if (success)
        {
            cpu.Run();
        }

        var hasError = false;

        sb.AppendLine("```ansi");

        foreach (var (range, allErrors) in builder.Errors.OrderBy(i => i.Key))
        {
            var errors = range.File.Scheme == "file"
                ? allErrors
                : allErrors.Where(i => i.Level != ErrorLevel.Debug);

            foreach (var error in errors)
            {
                hasError = true;

                sb.Append($"{error.Range.StartLine}:{error.Range.StartColumn} | ");

                var color = error.Level switch
                {
                    ErrorLevel.Error => "\u001b[0;31m",
                    ErrorLevel.Warning => "\u001b[0;33m",
                    _ => "\u001b[0;36m",
                };

                sb.Append(color);
                sb.Append(' ');
                sb.Append(error.Level);
                sb.Append("\u001b[0m");
                sb.Append(": ");
                sb.AppendLine(error.Message);
            }

            if (range.File.Scheme == "file")
            {
                sb.AppendLine();
                sb.AppendLine(code.GetPeek(range));
            }
        }

        if (success)
        {
            if (hasError)
            {
                sb.AppendLine();
            }

            sb.AppendLine("Variables:");

            var globalVariables = builder.Variables.Where(i => i is { IsGlobal: true, Identifier.Range.File.Scheme: "file" }).ToArray();

            if (globalVariables.Length > 0)
            {
                var memory = cpu.Banks[0];
                var length = globalVariables.Max(i => i.Identifier.Name.Length);

                foreach (var variable in globalVariables)
                {
                    var name = variable.Identifier.Name;
                    var range = variable.Identifier.Range;
                    var type = variable.Type;

                    sb.Append($"{range.StartLine}:{range.StartColumn} | ");
                    sb.Append(name);

                    if (name.Length < length)
                    {
                        sb.Append(' ', length - name.Length);
                    }

                    sb.Append(" = ");

                    if (variable.Pointer.Size > 1)
                    {
                        sb.Append('[');

                        for (var i = 0; i < variable.Pointer.Size; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }

                            sb.Append($"0x{memory[variable.Pointer.Address + i]:X4}");
                        }

                        sb.Append(']');
                    }
                    else
                    {
                        sb.Append($"0x{memory[variable.Pointer.Address]:X4}");
                    }

                    if (type is { StaticType: StaticType.Struct, StructReference: {} reference })
                    {
                        sb.Append(" \u001b[0;36m{ ");
                        for (var i = 0; i < reference.Fields.Count; i++)
                        {
                            if (i > 0)
                            {
                                sb.Append(", ");
                            }

                            var field = reference.Fields[i];

                            sb.Append(field.Name);
                            sb.Append(": ");

                            var value = memory[variable.Pointer.Address + field.Offset];

                            if (field.Bit is { } bit)
                            {
                                if (bit.Offset > 0)
                                {
                                    value >>= bit.Offset;
                                }

                                value &= (1 << bit.Size) - 1;
                            }

                            sb.Append($"0x{value:X4}");
                        }

                        sb.Append(" }\u001b[0;0m");
                    }

                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("```");
        
        Optional<IReadOnlyList<OneOf<FileData, IPartialAttachment>>> attachments = default;

        using var stream = new MemoryStream();

        if (handler.DidFlush)
        {
            handler.Image.Mutate(i => i.Resize(108 * 4, 108 * 4, KnownResamplers.NearestNeighbor));

            var isGif = handler.Image.Frames.Count > 1;

            if (isGif)
            {
                handler.Image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = 5;
                await handler.Image.SaveAsGifAsync(stream);
            }
            else
            {
                await handler.Image.SaveAsWebpAsync(stream);
            }

            stream.Position = 0;

            attachments = new(
                new OneOf<FileData, IPartialAttachment>[]
                {
                    new FileData(isGif ? "screen.gif" : "screen.webp", stream)
                }
            );
        }

        var result = await _channelApi.CreateMessageAsync(
            channelId,
            sb.Length == 14 ? default(Optional<string>) : sb.ToString(),
            attachments: attachments
        );

        if (!result.IsSuccess)
        {
            return Result.FromError(result);
        }

        return Result.FromSuccess();
    }
}
