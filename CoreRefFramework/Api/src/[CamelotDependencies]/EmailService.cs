using KAT.Camelot.Domain.Services;

namespace KAT.Camelot.Infrastructure.Services;

public class EmailService : IEmailService
{
	public EmailService() { }

	public string DefaultFrom => "a@b.com";
	public async Task<bool> SendEmailAsync( string to, string subject, string body, CancellationToken cancellationToken ) => await SendEmailAsync( to, null, subject, body, null, null, cancellationToken );
	public async Task<bool> SendEmailAsync( string to, string? from, string subject, string body, CancellationToken cancellationToken ) => await SendEmailAsync( to, from, subject, body, null, null, cancellationToken );
	public async Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, CancellationToken cancellationToken ) => await SendEmailAsync( to, from, subject, body, cc, null, cancellationToken );
	public async Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, string? bcc, CancellationToken cancellationToken )
	{
		await Task.Delay( 1, cancellationToken );
		return true;
	}
}