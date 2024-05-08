using FluentValidation;

#pragma warning disable IDE1006 // Naming rule violation

namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

public class xDSDataRequest : KatCredentialRequest
{
	public string Group { get; init; } = default!;
	public string AuthId { get; init; } = default!;
	public string Target { get; init; } = default!;
}

public class xDSDataRequestValidator : AbstractValidator<xDSDataRequest>
{
    public xDSDataRequestValidator()
    {
		RuleFor( r => r.Group ).NotNull();
		RuleFor( r => r.AuthId ).NotNull();
		RuleFor( r => r.Email ).NotNull();
		RuleFor( r => r.Password ).NotNull();
		RuleFor( r => r.Target ).NotNull();
	}
}