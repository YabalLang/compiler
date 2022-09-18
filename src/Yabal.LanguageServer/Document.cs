using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Astro8.Instructions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Serilog;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Yabal.LanguageServer;

public class Document
{
    private readonly TextDocumentContainer _documentContainer;
    private readonly ILanguageServerFacade _languageServer;
    private readonly string _uriString;

    public Document(DocumentUri uri, ILanguageServerFacade languageServer, TextDocumentContainer documentContainer)
    {
        _languageServer = languageServer;
        _documentContainer = documentContainer;
        Uri = uri;
        _uriString = uri.ToString();
    }

    public YabalBuilder Builder { get; set; }

    public DocumentUri Uri { get; }

    public int? Version { get; set; }

    public string Text { get; set; }

    public async Task UpdateAsync(int? version, string text)
    {
        if (Version >= version)
        {
            return;
        }

        Version = version;
        Text = text;

        try
        {
            var builder = new YabalBuilder();
            await builder.CompileCodeAsync(text);
            Builder = builder;

            var diagnostics = new List<Diagnostic>();

            foreach (var (range, level, message) in Builder.Errors.SelectMany(i => i.Value))
            {
                diagnostics.Add(new Diagnostic
                {
                    Message = message,
                    Range = new Range(new Position(range.StartLine - 1, range.StartColumn), new Position(range.EndLine - 1, range.EndColumn)),
                    Severity = level switch
                    {
                        ErrorLevel.Error => DiagnosticSeverity.Error,
                        ErrorLevel.Warning => DiagnosticSeverity.Warning,
                        _ => DiagnosticSeverity.Information
                    },
                });
            }

            builder.Build();

            _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = Uri,
                Diagnostics = diagnostics
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error compiling code");
        }
    }
}
