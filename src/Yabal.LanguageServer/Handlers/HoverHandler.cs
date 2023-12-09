using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Yabal.Ast;
using Yabal.Instructions;

namespace Yabal.LanguageServer.Handlers;

public class HoverHandler(TextDocumentContainer documentContainer) : IHoverHandler
{
    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        if (!documentContainer.Documents.TryGetValue(request.TextDocument.Uri, out var document))
        {
            return Task.FromResult<Hover?>(null);
        }

        var (identifier, variable) = document.Builder.Find(request.Position);

        if (identifier == null || variable == null)
        {
            return Task.FromResult<Hover?>(null);
        }

        var sb = new StringBuilder();
        var size = ((variable.Initializer as IConstantValue)?.Value as IAddress)?.Length ?? variable.Type.Size;

        sb.AppendLine($"Type: {variable.Type}  ");
        sb.AppendLine($"Size: {size}  ");

        if (variable.Pointer is not InstructionLabel)
        {
            sb.AppendLine($"Variable address: {variable.Pointer.Address}  ");
        }

        if (variable.Initializer is IPointerSource { Pointer: {} pointer })
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
            DocumentSelector = TextDocumentSelector.ForLanguage("yabal")
        };
    }
}
