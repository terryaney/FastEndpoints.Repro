using FluentValidation;

namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

public class DownloadDebugRequest : KatCredentialRequest
{
	public int VersionKey { get; init; }
}

public class DownloadDebugRequestValidator : AbstractValidator<DownloadDebugRequest>
{
    public DownloadDebugRequestValidator()
    {
		RuleFor( r => r.VersionKey ).NotNull().GreaterThan( 0 );
		RuleFor( r => r.Email ).NotNull();
		RuleFor( r => r.Password ).NotNull();
	}
}