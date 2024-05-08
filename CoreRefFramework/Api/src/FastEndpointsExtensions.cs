using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KAT.Camelot.Api.Excel;

// OBSOLETE: When I upgrade to latest FE
public static class FastEndpointsExtensions
{
	/// <summary>configure and enable jwt bearer authentication</summary>
	/// <param name="signingOptions">an action to configure <see cref="JwtSigningOptions" /></param>
	/// <param name="bearerOptions">an action to configure <see cref="JwtBearerOptions" /></param>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static IServiceCollection AddAuthenticationJwtBearer(
		this IServiceCollection services,
		Action<JwtSigningOptions> signingOptions,
		Action<JwtBearerOptions>? bearerOptions = null )
	{
		services
			.AddAuthentication( JwtBearerDefaults.AuthenticationScheme )
			.AddJwtBearer(
				o =>
				{
					var sOpts = new JwtSigningOptions();
					signingOptions( sOpts );

					SecurityKey? key = null;

					if ( sOpts.SigningKey is not null )
					{
						switch ( sOpts.SigningStyle )
						{
							case JWTBearer.TokenSigningStyle.Symmetric:
								key = new SymmetricSecurityKey( Encoding.ASCII.GetBytes( sOpts.SigningKey ) );

								break;
							case JWTBearer.TokenSigningStyle.Asymmetric:
							{
								var rsa = System.Security.Cryptography.RSA.Create(); //do not dispose
								if ( sOpts.KeyIsPemEncoded )
									rsa.ImportFromPem( sOpts.SigningKey );
								else
									rsa.ImportRSAPublicKey( Convert.FromBase64String( sOpts.SigningKey ), out _ );
								key = new RsaSecurityKey( rsa );

								break;
							}
							default:
								throw new InvalidOperationException( "Jwt signing style not specified!" );
						}
					}

					//set defaults
					o.TokenValidationParameters.IssuerSigningKey = key;
					o.TokenValidationParameters.ValidateIssuerSigningKey = key is not null;
					o.TokenValidationParameters.ValidateLifetime = true;
					o.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds( 60 );
					o.TokenValidationParameters.ValidAudience = null;
					o.TokenValidationParameters.ValidateAudience = false;
					o.TokenValidationParameters.ValidIssuer = null;
					o.TokenValidationParameters.ValidateIssuer = false;

					//set sensible defaults (based on configuration) for the claim mapping so tokens created with JWTBearer.CreateToken() will not be modified
					o.TokenValidationParameters.NameClaimType = "name"; // TODO: Conf.SecOpts.NameClaimType;
					o.TokenValidationParameters.RoleClaimType = "role"; // TODO: Conf.SecOpts.RoleClaimType;
					o.MapInboundClaims = false;

					bearerOptions?.Invoke( o );

					//correct any user mistake
					o.TokenValidationParameters.ValidateAudience = o.TokenValidationParameters.ValidAudience is not null;
					o.TokenValidationParameters.ValidateIssuer = o.TokenValidationParameters.ValidIssuer is not null;
				} );

		return services;
	}
}

// OBSOLETE: Remove this class and the AddAuthenticationJwtBearer extension when upgrade to latest FastEndpoints
public sealed class JwtSigningOptions
{
    /// <summary>
    /// the key used to sign jwts symmetrically or the public-key when jwts are signed symmetrically.
    /// the key can be optional when used to verify tokens issued by an idp where public key retrieval happens dynamically.
    /// </summary>
    /// <remarks>the key can be in PEM format. make sure to set <see cref="KeyIsPemEncoded" /> to <c>true</c> if the key is PEM encoded.</remarks>
    public string? SigningKey { get; set; }

    /// <summary>
    /// specifies how tokens were signed. symmetrically or asymmetrically.
    /// </summary>
    public JWTBearer.TokenSigningStyle SigningStyle { get; set; } = JWTBearer.TokenSigningStyle.Symmetric;

    /// <summary>
    /// specifies whether the key is pem encoded.
    /// </summary>
    public bool KeyIsPemEncoded { get; set; }
}