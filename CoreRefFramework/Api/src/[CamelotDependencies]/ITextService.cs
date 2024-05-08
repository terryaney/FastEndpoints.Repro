namespace KAT.Camelot.Domain.Services;

public interface ITextService
{
	Task<bool> SendAsync( string to, string from, string body, CancellationToken cancellationToken );
}