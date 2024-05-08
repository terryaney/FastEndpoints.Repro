using System.Diagnostics;
using KAT.Camelot.Domain.Extensions;
using Microsoft.AspNetCore.Http;

namespace KAT.Camelot.Domain.Web.Extensions;

public static class HttpClientExtensions
{
	public static Microsoft.AspNetCore.Mvc.ProblemDetails GetProblemDetails( this Exception ex, HttpContext context, Func<HttpContext, Exception, Dictionary<string, object?>?>? getExtensions = null )
	{
		var operationIsCancelled = ex is OperationCanceledException;

		var trace = new List<string>();
		var inner = ex;
		while ( inner != null )
		{
			trace.AddRange( inner.StackTrace?.Split( Environment.NewLine, StringSplitOptions.RemoveEmptyEntries ) ?? Array.Empty<string>() );
			inner = inner.InnerException;
		}

		var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails()
		{
			Title = "Internal Server Error",
			Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
			Detail = ex.Message,
			Status = operationIsCancelled ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError,
			Instance = context.Request.Path,
		};

		problemDetails.Extensions.Add( "traceId", Activity.Current?.TraceId.ToString() );
		problemDetails.Extensions.Add( "spanId", Activity.Current?.SpanId.ToString() );
		problemDetails.Extensions.Add( "requestId", context.TraceIdentifier );
		problemDetails.Extensions.Add( "machineName", Environment.MachineName );
		problemDetails.Extensions.Add( "exceptionType", ex.GetType().FullName );
		problemDetails.Extensions.Add( "stackTrace", trace.ToArray() );

		var problemExtensions = getExtensions?.Invoke( context, ex );

		if ( problemExtensions != null )
		{
			foreach ( var extension in problemExtensions.Where( e => e.Value != null ) )
			{
				problemDetails.Extensions.Add( PascalToCamelCase( extension.Key ), extension.Value );
			}
		}

		return problemDetails;
	}

	private static string PascalToCamelCase( string pascalName ) => char.ToLower( pascalName[ 0 ] ) + pascalName[ 1.. ];
}
