using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using Serilog;
using Yabal.LanguageServer;
using Yabal.LanguageServer.Handlers;
using Yabal.LanguageServer.Logging;

// System.Diagnostics.Debugger.Launch();

var server = await LanguageServer.From(options =>
{
    options
        .WithCommandLineCommunicationChannel(args)
        .WithHandler<TextDocumentHandler>()
        .WithHandler<HighlightHandler>()
        .WithHandler<HoverHandler>()
        .WithHandler<RenameHandler>()
        .WithHandler<DefinitionHandler>()
        .WithServices(services =>
        {
            services.AddSingleton<TextDocumentContainer>();
        })
        .OnInitialize((server, request, _) =>
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.LanguageServer(server)
                .CreateLogger();

            if (request.Capabilities?.TextDocument != null)
            {
                request.Capabilities.TextDocument.Rename = new RenameCapability
                {
                    PrepareSupport = true,
                    DynamicRegistration = true
                };

                request.Capabilities.TextDocument.DocumentHighlight = new DocumentHighlightCapability();
            }

            return Task.CompletedTask;
        });
});

await server.WaitForExit;
