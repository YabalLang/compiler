﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Yabal.LanguageServer.Handlers;

public class HighlightHandler(TextDocumentContainer documentContainer) : IDocumentHighlightHandler
{
    public Task<DocumentHighlightContainer?> Handle(DocumentHighlightParams request, CancellationToken cancellationToken)
    {
        var items = new List<DocumentHighlight>();

        if (documentContainer.Documents.TryGetValue(request.TextDocument.Uri, out var document))
        {
            AddHighlights(request, document, items);
        }

        return Task.FromResult<DocumentHighlightContainer?>(items);
    }

    private static void AddHighlights(TextDocumentPositionParams request, Document document, List<DocumentHighlight> items)
    {
        var (_, variable) = document.Builder.Find(request.Position);

        if (variable == null) return;

        items.Add(new DocumentHighlight
        {
            Kind = DocumentHighlightKind.Write,
            Range = variable.Identifier.Range.ToRange()
        });

        items.AddRange(variable.References.Select(i => new DocumentHighlight
        {
            Kind = DocumentHighlightKind.Read,
            Range = i.Range.ToRange()
        }));
    }

    public DocumentHighlightRegistrationOptions GetRegistrationOptions(DocumentHighlightCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentHighlightRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("yabal")
        };
    }
}
