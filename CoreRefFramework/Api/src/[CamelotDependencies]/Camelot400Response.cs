using System.Diagnostics.CodeAnalysis;

namespace KAT.Camelot.Api.Responses;

[ExcludeFromCodeCoverage( Justification = "Only used to generate Swagger documentation" )]
public class Camelot400Response : Camelot400Response<ApiProblem>
{
}

[ExcludeFromCodeCoverage( Justification = "Only used to generate Swagger documentation" )]
public class Camelot400Response<TApiProblem>
{
	public ValidationIssues? ValidationIssues { get; init; }
	public TApiProblem? ApiProblem { get; init; } 
}