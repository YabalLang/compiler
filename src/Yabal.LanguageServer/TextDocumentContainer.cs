using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Yabal.LanguageServer;

public class TextDocumentContainer(ILanguageServerFacade server)
{
    public ILanguageServerFacade Server { get; } = server;

    public ConcurrentDictionary<DocumentUri, Document> Documents { get; } = new();

    public Document Get(DocumentUri uri)
    {
        return Documents.AddOrUpdate(
            uri,
            u => new Document(u, Server, this),
            (_, document) => document
        );
    }

    public async Task Update(DocumentUri uri, int? version, string text)
    {
        var document = Get(uri);

        await document.UpdateAsync(version, text);
    }
}
