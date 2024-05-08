using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KAT.Camelot.Api;

public class UpdatableJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
	private readonly IConfiguration configuration;
	private readonly IHttpContextAccessor httpContextAccessor;
	private readonly Dictionary<string, string[]> jwtTypes;
	private readonly Dictionary<string, string[]>? alternateJwtTypes;

	public UpdatableJwtBearerOptions( IConfiguration configuration, IHttpContextAccessor httpContextAccessor, Dictionary<string, string[]> jwtTypes, Dictionary<string, string[]>? alternateJwtTypes = null )
	{
		// TheKeep didn't update it's settings when file changed
		// https://github.com/dotnet/aspnetcore/issues/49586
		this.configuration = configuration;
		this.httpContextAccessor = httpContextAccessor;
		this.jwtTypes = jwtTypes;
		this.alternateJwtTypes = alternateJwtTypes;
	}

	// https://github.com/dotnet/aspnetcore/issues/21491
	// https://github.com/aspnet/Security/issues/1709
	// https://stackoverflow.com/questions/54167610/asp-net-core-change-jwt-securitykey-during-runtime
	public void Configure( string? name, JwtBearerOptions options )
	{
		options.TokenValidationParameters.IssuerSigningKeyResolver = ( token, securityToken, kid, validationParameters ) =>
		{
			var ctx = httpContextAccessor.HttpContext!;
			var path = ctx.Request.Path;
			var jwtType = jwtTypes.Count == 1
				? jwtTypes.Keys.First()
				: jwtTypes.Keys.FirstOrDefault( k => jwtTypes[ k ].Any( p => path!.StartsWithSegments( p ) ) );

			if ( ( alternateJwtTypes?.Any() ?? false ) && ctx.Request.Headers.TryGetValue( "x-kat-alternate-jwt", out var h ) && string.Compare( h.FirstOrDefault(), "true", true ) == 0 )
			{
				jwtType = alternateJwtTypes.Keys.FirstOrDefault( k => alternateJwtTypes[ k ].Any( p => path!.StartsWithSegments( p ) ) );
			}

			validationParameters.ValidIssuer =
				configuration.GetValue<string>( $"TheKeep:Jwt:{jwtType}:Issuer" ) ??
				configuration.GetValue<string>( $"MyKeep:Jwt:{jwtType}:Issuer" );

			var secret =
				configuration.GetValue<string>( $"TheKeep:Jwt:{jwtType}:Secret" ) ??
				configuration.GetValue<string>( $"MyKeep:Jwt:{jwtType}:Secret" )!;

			return new List<SecurityKey>() 
			{ 
				new SymmetricSecurityKey( Encoding.ASCII.GetBytes( secret ) )
			};
		};
	}

	[ExcludeFromCodeCoverage( Justification = "Interface method not used/supported" )]
	public void Configure( JwtBearerOptions options ) => throw new NotImplementedException();
}