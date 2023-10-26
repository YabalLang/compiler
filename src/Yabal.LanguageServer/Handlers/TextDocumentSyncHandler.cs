using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Yabal.LanguageServer.Handlers;

internal class TextDocumentHandler : TextDocumentSyncHandlerBase
{
    private readonly TextDocumentContainer _documentContainer;

    public TextDocumentHandler(TextDocumentContainer documentContainer)
    {
        _documentContainer = documentContainer;
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        return new TextDocumentAttributes(uri, "yabal");
    }

    public override async Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        await _documentContainer.Update(
            request.TextDocument.Uri,
            request.TextDocument.Version,
            request.TextDocument.Text
        );

        return Unit.Value;
    }

    public override async Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var text = request.ContentChanges.FirstOrDefault()?.Text;

        if (text == null)
        {
            return Unit.Value;
        }

        await _documentContainer.Update(
            request.TextDocument.Uri,
            request.TextDocument.Version,
            text
        );

        return Unit.Value;
    }

    public override Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    public override Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSyncRegistrationOptions
        {
            Change = TextDocumentSyncKind.Full,
            DocumentSelector = TextDocumentSelector.ForLanguage("yabal"),
            Save = new SaveOptions { IncludeText = true }
        };
    }
}
