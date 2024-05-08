namespace KAT.Camelot.Domain.Services;

public interface IEmailService
{
	string DefaultFrom { get; }
	Task<bool> SendEmailAsync( string to, string subject, string body, CancellationToken cancellationToken );
	Task<bool> SendEmailAsync( string to, string? from, string subject, string body, CancellationToken cancellationToken );
	Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, CancellationToken cancellationToken );
	Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, string? bcc, CancellationToken cancellationToken );
}