using KAT.Camelot.Domain.Services;

namespace KAT.Camelot.Testing.Integration;

public class FakeEmailService : IEmailService
{
	public string DefaultFrom => "fake.service@conduent.com";

	public class Message
	{
		public required string To { get; init; }
		public string? From { get; init; }
		public required string Subject { get; init; }
		public required string Body { get; init; }
		public string? CC { get; init; }
	}

	public List<Message> SentMessages { get; private init; } = new List<Message>();
	public void ClearSentMessages() => SentMessages.Clear();

	public Task<bool> SendEmailAsync( string to, string subject, string body, CancellationToken cancellationToken ) => SendEmailAsync( to, null, subject, body, null, null, cancellationToken );
	public Task<bool> SendEmailAsync( string to, string? from, string subject, string body, CancellationToken cancellationToken ) => SendEmailAsync( to, from, subject, body, null, null, cancellationToken );
	public Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, CancellationToken cancellationToken ) => SendEmailAsync( to, from, subject, body, cc, null, cancellationToken );

	public Task<bool> SendEmailAsync( string to, string? from, string subject, string body, string? cc, string? bcc, CancellationToken cancellationToken )
	{
		if ( to.IndexOf( "throw.exception@conduent.com" ) > -1 )
		{
			throw new ApplicationException( "Unable to send email" );
		}
		
		SentMessages.Add( new Message { To = to, From = from ?? DefaultFrom, Subject = subject, Body = body, CC = cc } );
		return Task.FromResult( true );
	}
}