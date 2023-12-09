using System;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Yabal.LanguageServer.Logging;

public class LanguageServerSink(IFormatProvider? formatProvider, ILanguageServerFacade server)
    : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(formatProvider);
        server.SendNotification("yabal/log", message);
    }
}

public static class LanguageServerSinkExtensions
{
    public static LoggerConfiguration LanguageServer(
        this LoggerSinkConfiguration loggerConfiguration,
        ILanguageServerFacade server,
        IFormatProvider? formatProvider = null)
    {
        return loggerConfiguration.Sink(new LanguageServerSink(formatProvider, server));
    }
}
