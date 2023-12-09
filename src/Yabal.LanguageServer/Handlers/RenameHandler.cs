using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Yabal.LanguageServer.Handlers;

public class RenameHandler(TextDocumentContainer documentContainer) : IRenameHandler
{
	public Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
	{
		if (!documentContainer.Documents.TryGetValue(request.TextDocument.Uri, out var document))
		{
			return Task.FromResult<WorkspaceEdit?>(null);
		}

		var (_, variable) = document.Builder.Find(request.Position);

		if (variable == null)
		{
			return Task.FromResult<WorkspaceEdit?>(null);
		}

		return Task.FromResult<WorkspaceEdit?>(new WorkspaceEdit
		{
			Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>
			{
				[request.TextDocument.Uri] =
				[
					..variable.References
						.OrderByDescending(i => i.Range)
						.DistinctBy(i => i.Range.Index)
						.Select(i => new TextEdit
						{
							Range = i.Range.ToRange(),
							NewText = request.NewName
						}),
					new TextEdit
					{
						Range = variable.Identifier.Range.ToRange(),
						NewText = request.NewName
					}
				]
			}
		});
	}

	public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability, ClientCapabilities clientCapabilities)
	{
		return new RenameRegistrationOptions
		{
			DocumentSelector = TextDocumentSelector.ForLanguage("yabal")
		};
	}
}
