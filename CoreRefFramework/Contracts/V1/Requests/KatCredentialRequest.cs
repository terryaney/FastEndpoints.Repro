using FastEndpoints;

namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

public class KatCredentialRequest
{
	[FromHeader( "X-KAT-Email" )]
	public string Email { get; init; } = default!;
	
	[FromHeader( "X-KAT-Password" )]
	public string Password { get; init; } = default!;
}
