using FastEndpoints;
using FluentValidation;

namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

public class CalcEngineRequest : KatCredentialRequest
{
	public string Name { get; init; } = default!;
}

public class CalcEngineRequestValidator : AbstractValidator<CalcEngineRequest>
{
    public CalcEngineRequestValidator()
    {
		RuleFor( r => r.Name ).NotNull();
		RuleFor( r => r.Email ).NotNull();
		RuleFor( r => r.Password ).NotNull();
	}
}