using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace KAT.Camelot.Api.Responses;


[ExcludeFromCodeCoverage( Justification = "Only used to generate Swagger documentation" )]
public class ApiProblem : ProblemDetails
{
	public required string TraceId { get; init; }
	public required string RequestId { get; init; } 
}