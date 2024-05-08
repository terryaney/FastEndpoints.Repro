using KAT.Camelot.Domain.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace KAT.Camelot.Api;

// https://benfoster.io/blog/customize-authorization-response-aspnet-core/
public partial class AuthorizationForbiddenHeaderHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _handler;
	private readonly ILogger<AuthorizationMiddlewareResultHandler> logger;

	public AuthorizationForbiddenHeaderHandler( ILogger<AuthorizationMiddlewareResultHandler> logger )
    {
        _handler = new AuthorizationMiddlewareResultHandler();
		this.logger = logger;
	}

	[LoggerMessage( 1, LogLevel.Warning, "{method} {path} JWT {claimName} Authorization failed." )]
	partial void LogWarningJwtAuthorizationFailed( string method, string path, string claimName );

	public async Task HandleAsync( RequestDelegate requestDelegate, HttpContext httpContext, AuthorizationPolicy authorizationPolicy, PolicyAuthorizationResult policyAuthorizationResult )
	{
		if ( policyAuthorizationResult.Forbidden && policyAuthorizationResult.AuthorizationFailure?.FailedRequirements.FirstOrDefault( r => r is ClaimsAuthorizationRequirement ) is ClaimsAuthorizationRequirement claimFailed )
		{
			httpContext.Response.Headers.Add( ApiResponseHeaders.Jwt.Failed.Header, "true" ); // used for logging, check original implementation
			httpContext.Response.Headers.Add( ApiResponseHeaders.Jwt.Failed.Detail, $"{claimFailed.ClaimType} claim invalid." );

			var th = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
			var token = th.ReadJwtToken( httpContext.Request.Headers.Authorization.ToString()[ "Bearer ".Length.. ] );
			var payload = token.ToString();
			// LogContext.PushProperty( "payload", payload );
			httpContext.Response.Headers.Append( ApiResponseHeaders.Jwt.Failed.Payload, payload );
			LogWarningJwtAuthorizationFailed( httpContext.Request.Method, httpContext.Request.Path, claimFailed.ClaimType );
		}

		await _handler.HandleAsync( requestDelegate, httpContext, authorizationPolicy, policyAuthorizationResult );
	}
}