using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Yabal.Ast;

namespace Yabal.LanguageServer.Handlers;

public class HoverHandler : IHoverHandler
{
    private readonly TextDocumentContainer _documentContainer;

    public HoverHandler(TextDocumentContainer documentContainer)
    {
        _documentContainer = documentContainer;
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        if (!_documentContainer.Documents.TryGetValue(request.TextDocument.Uri, out var document))
        {
            return Task.FromResult<Hover?>(null);
        }

        var (identifier, variable) = document.Builder.Variables.Find(request.Position);

        if (identifier == null || variable == null)
        {
            return Task.FromResult<Hover?>(null);
        }

        var sb = new StringBuilder();
        var size = ((variable.Initializer as IConstantValue)?.Value as IAddress)?.Length ?? variable.Type.Size;

        sb.AppendLine($"Type: {variable.Type}  ");
        sb.AppendLine($"Size: {size}  ");
        sb.AppendLine($"Variable address: {variable.Pointer.Address}  ");

        if (variable.Initializer is AddressExpression { Pointer: {} pointer })
        {
            sb.AppendLine($"Pointer address: {pointer.Address}  ");
            sb.AppendLine($"Pointer bank: {pointer.Bank}  ");
        }
        else if (variable.Initializer is IConstantValue {Value: { } value})
        {
            sb.AppendLine($"Value: {value}  ");
        }

        return Task.FromResult<Hover?>(new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = sb.ToString()
            }),
            Range = identifier.Range.ToRange()
        });
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
    {
        return new HoverRegistrationOptions
        {
            DocumentSelector = DocumentSelector.ForLanguage("yabal")
        };
    }
}
