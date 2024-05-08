using System.Text.Json.Nodes;
using FastEndpoints;
using FluentValidation;

namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

public class EmailBlastRequest
{
	public int AddressesPerEmail { get; init; }
	public int WaitPerBatch { get; init; }
	public string? Bcc { get; init; }
	public string From { get; init; } = null!;
	public string Subject { get; init; } = null!;
	public string? Audit { get; init; }
	// Property setter so that I can reassign when I parse <img src=""/>
	public EmailBlastAttachment[] Attachments { get; set; } = Array.Empty<EmailBlastAttachment>();
	public JsonArray Recipients { get; init; } = new JsonArray();
}

public class EmailBlastAttachment
{
	public string Id { get; init; } = null!;
	public string Name { get; init; } = null!;
	public string? ContentId { get; init; }
}

public class EmailBlastRequestValidator : AbstractValidator<EmailBlastRequest>
{
	public EmailBlastRequestValidator()
	{
		RuleFor( r => r.AddressesPerEmail ).NotEmpty().InclusiveBetween( 1, 1000 );
		RuleFor( r => r.WaitPerBatch ).NotEmpty().InclusiveBetween( 0, 5 );

		RuleFor( r => r.From )
			.NotEmpty()
			.Must( f => System.Net.Mail.MailAddress.TryCreate( f, out var _ ) )
			.WithMessage( "{PropertyName} is not a valid email address." );

		RuleFor( r => r.Subject ).NotEmpty();
		
		RuleFor( r => r.Recipients )
			.NotEmpty()
			.Must( recipients => recipients.All( r =>  System.Net.Mail.MailAddress.TryCreate( (string?)r![ "c0" ], out var _ ) ) )
			.WithMessage( ( r, recipients ) => $"One or more emails are not a valid email addresses.  The first offending email is: {recipients.First( r => !System.Net.Mail.MailAddress.TryCreate( (string?)r![ "c0" ], out var _ ) )![ "c0" ]}" );

		RuleFor( r => r.Bcc )
			.Must( bcc => string.IsNullOrWhiteSpace( bcc ) || bcc.Split( ';' ).All( x => System.Net.Mail.MailAddress.TryCreate( x, out var _ ) ) )
			.WithMessage( "{PropertyName} is not a valid email address." );
	}
}