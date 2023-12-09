using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Yabal.LanguageServer.Handlers;

public class DefinitionHandler(TextDocumentContainer documentContainer) : IDefinitionHandler
{
	public Task<LocationOrLocationLinks?> Handle(DefinitionParams request, CancellationToken cancellationToken)
	{
		if (!documentContainer.Documents.TryGetValue(request.TextDocument.Uri, out var document))
		{
			return Task.FromResult<LocationOrLocationLinks?>(null);
		}

		var (_, variable) = document.Builder.Find(request.Position);

		if (variable == null)
		{
			return Task.FromResult<LocationOrLocationLinks?>(null);
		}

		return Task.FromResult<LocationOrLocationLinks?>(new LocationOrLocationLinks(new LocationOrLocationLink(new Location
		{
			Uri = request.TextDocument.Uri,
			Range = variable.Identifier.Range.ToRange()
		})));
	}

	public DefinitionRegistrationOptions GetRegistrationOptions(DefinitionCapability capability,
		ClientCapabilities clientCapabilities)
	{
		return new DefinitionRegistrationOptions
		{
			DocumentSelector = TextDocumentSelector.ForLanguage("yabal")
		};
	}
}
