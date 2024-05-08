using System.Diagnostics.CodeAnalysis;
using FastEndpoints;
using KAT.Camelot.Domain.Http;
using Microsoft.AspNetCore.Http;
using NJsonSchema;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace KAT.Camelot.Api;

// Adapted from https://github.com/mattfrear/Swashbuckle.AspNetCore.Filters/tree/master/src/Swashbuckle.AspNetCore.Filters/ResponseHeaders
[ExcludeFromCodeCoverage( Justification = "Only used to generate Swagger documentation" )]
public class AddResponseHeadersProcessor : IOperationProcessor
{
	public bool Process( OperationProcessorContext context ) 
	{
		var feContext = context as AspNetCoreOperationProcessorContext;

		var response = context.OperationDescription.Operation.Responses[ StatusCodes.Status400BadRequest.ToString() ];
		var headers = response.Headers;
		headers.Add(
			ApiResponseHeaders.Jwt.Failed.Header,
			new NSwag.OpenApiHeader
			{
				Description = "Optional header injected as 'true' when JWT validation fails.",
				Type = JsonObjectType.String
			}
		);
		headers.Add(
			ApiResponseHeaders.Jwt.Failed.Detail,
			new NSwag.OpenApiHeader
			{
				Description = "Optional header injected with some detail as to why the JWT validation fails.",
				Type = JsonObjectType.String
			}
		);
		headers.Add(
			ApiResponseHeaders.Jwt.Failed.Dates,
			new NSwag.OpenApiHeader
			{
				Description = "Optional header injected with date details to help debug issues with server skew time issues.",
				Type = JsonObjectType.String
			}
		);
		headers.Add(
			ApiResponseHeaders.Jwt.Expired,
			new NSwag.OpenApiHeader
			{
				Description = "Optional header injected as 'true' when the JWT token has expired.",
				Type = JsonObjectType.String
			}
		);

		var endpointDefinition = feContext?.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault( d => d is EndpointDefinition ) as EndpointDefinition;

		// Should put some attributes on endpoint or something...hack below will suffice
		if ( ( endpointDefinition?.EndpointType.FullName ?? "" ).IndexOf( ".DataLocker" ) > -1 )
		{
			var nameParts = endpointDefinition!.EndpointType.FullName!.Split( '.' );

			if ( nameParts.Any( p => p.StartsWith( "Download" ) ) )
			{
				response = context.OperationDescription.Operation.Responses[ StatusCodes.Status200OK.ToString() ];
				headers = response.Headers;
				headers.Add(
					ApiResponseHeaders.DataLocker.FileVersion,
					new NSwag.OpenApiHeader
					{
						Description = "The version of the downloaded file.",
						Type = JsonObjectType.String
					}
				);
			}

			if ( nameParts.Any( p => p.StartsWith( "DeleteAll" ) ) )
			{
				response = context.OperationDescription.Operation.Responses[ StatusCodes.Status200OK.ToString() ];
				headers = response.Headers;
				headers.Add(
					ApiResponseHeaders.DataLocker.FilesDeleted,
					new NSwag.OpenApiHeader
					{
						Description = "The number of files deleted.",
						Type = JsonObjectType.String
					}
				);
			}
		}
		return true;
	}
}