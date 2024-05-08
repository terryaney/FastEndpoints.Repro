using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using FastEndpoints;
using KAT.Camelot.Api.Responses;
using Microsoft.AspNetCore.Http;

namespace KAT.Camelot.Api;

/*
public abstract class CamelotEndpointWithoutRequest<TResponse> : CamelotEndpoint<EmptyRequest, TResponse>
{
	/// <summary>
	/// the handler method for the endpoint. this method is called for each request received.
	/// </summary>
	/// <param name="ct">a cancellation token</param>
	public virtual Task HandleAsync( CancellationToken ct ) => throw new NotImplementedException();

	/// <summary>
	/// override the HandleAsync(CancellationToken ct) method instead of using this method!
	/// </summary>
	public sealed override Task HandleAsync( EmptyRequest _, CancellationToken ct ) => HandleAsync( ct );
}
*/

[ExcludeFromCodeCoverage( Justification = "Fastendpoint without TResponse pass-thru." )]
public abstract class CamelotEndpointWithoutRequest : CamelotEndpoint<EmptyRequest, object> { }

[ExcludeFromCodeCoverage( Justification = "Fastendpoint without TResponse pass-thru." )]
public abstract class CamelotEndpointWithoutRequest<TResponse> : CamelotEndpoint<EmptyRequest, TResponse> { }

[ExcludeFromCodeCoverage( Justification = "Fastendpoint without TResponse pass-thru." )]
public abstract class CamelotEndpoint<TRequest> : CamelotEndpoint<TRequest, object> where TRequest : notnull { }

public abstract class CamelotEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull
{
	protected virtual void Describe( 
		string summary, 
		string swaggerTag, 
		string instance, 
		string validationErrors,
		string problemDetail,
		string? problemExtensions = null,
		Action<EndpointSummary>? responseDescriptionBuilder = null
	)
	{
		Description( builder =>
			{
				builder.WithTags( swaggerTag )
					.Produces( StatusCodes.Status401Unauthorized )
					.Produces( StatusCodes.Status403Forbidden )
					.Produces<Camelot400Response<ApiProblem>>( StatusCodes.Status400BadRequest, "application/json+problem" );

				if ( Definition.FormDataContentType != null )
				{
					builder.Accepts<TRequest>( Definition.FormDataContentType );
				}
				else if ( Definition.Verbs?.Any( m => m is "GET" or "HEAD" or "DELETE" or "PATCH" ) ?? true )
				{
					// https://discord.com/channels/933662816458645504/1085526696406548604/1085573095949078548
					// https://discord.com/channels/933662816458645504/1085526696406548604/1085574232840347669
					builder.Accepts<TRequest>( "*/*", "application/json" );
				}
				else
				{
					builder.Accepts<TRequest>( "application/json" );
				}

				if ( typeof( TResponse ) == typeof( object ) )
				{
					builder.Produces( StatusCodes.Status200OK );
				}
				else
				{
					builder.Produces<TResponse>( StatusCodes.Status200OK );
				}
			},
			true
		);

		Summary( s =>
		{
			s.Summary = summary;

			s.Responses[ StatusCodes.Status400BadRequest ] = "ValidationIssues object is returned when 400/BadRequest happens. ApiProblem object is returned with Status set to 500/InternalServerError, 401/Unauthorized, or 403/Forbidden occurs.";
			s.ResponseExamples[ StatusCodes.Status400BadRequest ] = $$"""
{
    "ValidationIssues.Response": {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "One or more validation errors occurred.",
        "status": 400,
        "instance": "{{instance}}",
        "errors": {{validationErrors}},
        "traceId": "0HMOCI6B16VK1:00000001",
        "requestId": "00-0HMOCI6B16VK1:00000001-00"
    },
    "ApiProblem.Response": {
		"type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
		"title": "Internal Server Error",
		"status": 500,
		"detail": "{{problemDetail}}",
		"instance": "{{instance}}",
		"traceId": "800001ba-0000-f600-b63f-84710c7967bb",
		"requestId": "00-ab11529e611145cd9c5aa958a7f2971c-376dd939fbd1c512-00"
		{{problemExtensions}}
	}
}
""";

			responseDescriptionBuilder?.Invoke( s );
		} );
	}

	protected string? AuthorizedUserEmail => User.FindFirst( ClaimTypes.Email )?.Value;
	protected string? AuthorizedUserName => User.FindFirst( ClaimTypes.NameIdentifier )?.Value;
}