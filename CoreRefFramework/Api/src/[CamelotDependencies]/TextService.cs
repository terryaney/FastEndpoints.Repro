using KAT.Camelot.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KAT.Camelot.Infrastructure.Services;

public class TextService : ITextService
{
	public TextService()
	{
	}

	public async Task<bool> SendAsync( string to, string from, string body, CancellationToken cancellationToken )
	{
		await Task.Delay( 1, cancellationToken );
		return true;
	}
}