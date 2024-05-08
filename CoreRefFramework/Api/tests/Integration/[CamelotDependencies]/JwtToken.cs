using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Tokens;

namespace KAT.Camelot.Domain.Security.Identity;

public class JwtToken : IDisposable
{
	private readonly SecurityToken? securityToken;
	private readonly JwtSecurityTokenHandler tokenHandler;
	private readonly RSACryptoServiceProvider? rsaCsp;
	private bool disposed;

	JwtToken()
	{
		tokenHandler = new JwtSecurityTokenHandler();

		// Was getting validation issues from India Devs...
		// Site Debug Creates Token -> QA Proxy Api Fails Authentication:
		/*
        Bearer was not authenticated. Failure message: IDX10222: Lifetime validation failed. The token is not yet valid. ValidFrom: '2/20/2023 3:55:57 PM', Current time: '2/20/2023 3:55:48 PM'.

        Failed to validate the token.

        Exception: 
        Microsoft.IdentityModel.Tokens.SecurityTokenNotYetValidException: IDX10222: Lifetime validation failed. The token is not yet valid. ValidFrom: '2/20/2023 3:55:57 PM', Current time: '2/20/2023 3:55:48 PM'.
        at Microsoft.IdentityModel.Tokens.Validators.ValidateLifetime(Nullable`1 notBefore, Nullable`1 expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
        at System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.ValidateTokenPayload(JwtSecurityToken jwtToken, TokenValidationParameters validationParameters, BaseConfiguration configuration)
        at System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.ValidateJWS(String token, TokenValidationParameters validationParameters, BaseConfiguration currentConfiguration, SecurityToken& signatureValidatedToken, ExceptionDispatchInfo& exceptionThrown)
        --- End of stack trace from previous location ---
        at System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.ValidateToken(String token, JwtSecurityToken outerToken, TokenValidationParameters validationParameters, SecurityToken& signatureValidatedToken)
        at System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.ValidateToken(String token, TokenValidationParameters validationParameters, SecurityToken& validatedToken)
        at Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler.HandleAuthenticateAsync()
        */

		// https://stackoverflow.com/a/49304699/166231
		// tokenHandler.SetDefaultTimesOnTokenCreation = false;
        // Update: Solution to this problem was to set ClockSkew to 60 seconds.
	}

	public JwtToken( string securityKey, string jwtToken ) : this()
	{
		var mySecurityKey = new SymmetricSecurityKey( Encoding.ASCII.GetBytes( securityKey ) );

		try
		{
			tokenHandler.ValidateToken( jwtToken, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = true,
				IssuerSigningKey = mySecurityKey,
				ClockSkew = TimeSpan.FromSeconds( 60 )
			}, out securityToken );
		}
		catch
		{
		}
	}

	public JwtToken( string securityKey, string issuer, string jwtToken ) : this()
	{
		var mySecurityKey = new SymmetricSecurityKey( Encoding.ASCII.GetBytes( securityKey ) );

		try
		{
			tokenHandler.ValidateToken( jwtToken, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				ValidateIssuer = true,
				ValidateAudience = false,
				ValidIssuer = issuer,
				ValidateLifetime = true,
				IssuerSigningKey = mySecurityKey,
				ClockSkew = TimeSpan.FromSeconds( 60 )
			}, out securityToken );
		}
		catch
		{
		}
	}

	public JwtToken( string securityKey, string issuer, Dictionary<string, string> claims ) : this( securityKey, issuer, claims, null ) { }
	public JwtToken( string securityKey, string issuer, DateTime expires ) : this( securityKey, issuer, new Dictionary<string, string> { { ClaimTypes.Realm.Admittance, "true" } }, expires ) { }
	public JwtToken( string securityKey, string issuer, Dictionary<string, string> claims, DateTime? expires ) : this()
	{
		var mySecurityKey = new SymmetricSecurityKey( Encoding.ASCII.GetBytes( securityKey ) );
		var notBefore = expires != null && expires < DateTime.UtcNow ? expires.Value.AddMilliseconds( -1 ) : (DateTime?)null;

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity( claims.Keys.Select( k => new Claim( k, claims[ k ] ) ) ),
			Expires = expires,
			NotBefore = notBefore,
			Issuer = issuer,
			SigningCredentials = new SigningCredentials( mySecurityKey, SecurityAlgorithms.HmacSha256Signature )
		};
        
		securityToken = tokenHandler.CreateToken( tokenDescriptor );
	}

	public JwtToken( RSACryptoServiceProvider rsaCsp, string issuer, Dictionary<string, object> claims ) : this()
	{
		this.rsaCsp = rsaCsp;

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity( claims.Keys.Select( k =>
			{
				if ( claims[ k ] is JsonArray array )
				{
					return new Claim( k, array.ToJsonString(), JsonClaimValueTypes.JsonArray );
				}
				else if ( claims[ k ] is JsonObject json )
				{
					return new Claim( k, json.ToJsonString(), JsonClaimValueTypes.Json );
				}
				else
				{
					return new Claim( k, ( claims[ k ] as string )! );
				}
			} ) ),
			Issuer = issuer,
			SigningCredentials = new SigningCredentials( new RsaSecurityKey( rsaCsp ), SecurityAlgorithms.RsaSha256Signature )
		};

		securityToken = tokenHandler.CreateToken( tokenDescriptor );
	}

	public async static Task<JwtToken> CreateRsaAsync( string pemFile, string issuer, Dictionary<string, object> claims )
	{
		using var f = File.OpenText( pemFile );
		using var sr = new StringReader( await f.ReadToEndAsync() );

		await sr.ReadLineAsync();
		var pemContent = await sr.ReadToEndAsync();
		pemContent = pemContent[ ..pemContent.LastIndexOf( Environment.NewLine ) ];
		var pemBody = Convert.FromBase64String( pemContent );

		using var rsa = RSA.Create();

		rsa.ImportRSAPrivateKey( pemBody, out _ );
		var rsaParams = rsa.ExportParameters( true );
		var rsaCsp = new RSACryptoServiceProvider();
		rsaCsp.ImportParameters( rsaParams );
		return new JwtToken( rsaCsp, issuer, claims );
	}

	public override string? ToString() => securityToken?.ToString();

	public string WriteToken() => tokenHandler.WriteToken( securityToken );
	public bool ValidateToken() => securityToken != null;

	public string GetClaim( string claimType )
	{
		if ( !ValidateToken() )
		{
			throw new UnauthorizedAccessException( "JWT is not valid." );
		}

		var stringClaimValue = ( securityToken as JwtSecurityToken )!.Claims.First( claim => claim.Type == claimType ).Value;
		return stringClaimValue;
	}

	protected virtual void Dispose( bool disposing )
	{
		if ( !disposed )
		{
			if ( disposing )
			{
				rsaCsp?.Dispose();
			}

			disposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}
}