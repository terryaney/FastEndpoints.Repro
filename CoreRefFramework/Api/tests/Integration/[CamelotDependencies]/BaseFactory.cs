using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using KAT.Camelot.Domain.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using KAT.Camelot.Domain.Security.Identity;
using KAT.Camelot.Domain.Configuration;

namespace KAT.Camelot.Testing.Integration;

public abstract class BaseFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
	protected Jwt jwt = null!;

	public virtual string UserName => "integration.tester";
	public virtual string UserEmail => $"{UserName}@integration.com";

	public FakeEmailService EmailService { get; } = new FakeEmailService();
	public FakeDateTimeService DateTimeService { get; } = new FakeDateTimeService();

	protected virtual void ProcessConfiguration( IConfigurationRoot configuration ) { }
	protected virtual void ConfigureTestServices( IServiceCollection services ) { }
	protected virtual void ConfigureAppConfiguration( WebHostBuilderContext context, IConfigurationBuilder config ) { }

	protected override void ConfigureWebHost( IWebHostBuilder builder )
	{
		// Set any needed environment variables, by default asp.net core environment is 'default'
		Environment.SetEnvironmentVariable( "CAMELOT_SECRETS_ENVIRONMENT", "Integration" );

		builder.ConfigureAppConfiguration( ( context, config ) =>
		{
			ConfigureAppConfiguration( context, config );

			// https://stackoverflow.com/a/72824811/166231
			// Supposedly ok to call config.Build() multiple times
			var siteConfig = config.Build();
			jwt = siteConfig.GetSection( "TheKeep" ).GetSection( "Jwt" ).Get<Jwt>()!;
			ProcessConfiguration( siteConfig );
		} );

		builder.ConfigureTestServices( services =>
		{
			// Recommended by Chapsas...doubt any present in this project, but just want here for reference
			services.RemoveAll( typeof( IHostedService ) );

			// Disable logging...
			services.AddSingleton<ILoggerFactory, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory>();

			services.AddScoped<IEmailService>( services => EmailService );
			services.AddScoped<IDateTimeService>( services => DateTimeService );

			ConfigureTestServices( services );
		});

		base.ConfigureWebHost( builder );
	}

	protected HttpClient CreateClient( Func<JwtInfo> getJwtInfo )
	{
		var claims = new Dictionary<string, string> {
			{ ClaimTypes.Realm.Admittance, "true" },
			{ System.Security.Claims.ClaimTypes.NameIdentifier, UserName },
			{ System.Security.Claims.ClaimTypes.Email, UserEmail }
		};

		return CreateClient( getJwtInfo, claims );
	}

	protected HttpClient CreateClient( Func<JwtInfo> getJwtInfo, bool admittance, string? name, string? email )
	{
		var claims = new Dictionary<string, string> { { ClaimTypes.Realm.Admittance, admittance.ToString() } };

		if ( name != null )
		{
			claims.Add( System.Security.Claims.ClaimTypes.NameIdentifier, name );
		}
		if ( email != null )
		{
			claims.Add( System.Security.Claims.ClaimTypes.Email, email );
		}

		return CreateClient( getJwtInfo, claims );
	}

	protected HttpClient CreateClient( Func<JwtInfo> getJwtInfo, Dictionary<string, string> claims, bool includeCoreClaims = true )
	{
		var jwtClaims = includeCoreClaims
			? new Dictionary<string, string>()
			{
				{ ClaimTypes.Realm.Admittance, "true" },
				{ System.Security.Claims.ClaimTypes.NameIdentifier, UserName },
				{ System.Security.Claims.ClaimTypes.Email, UserEmail }
			}
			: new Dictionary<string, string>();

		foreach ( var claim in claims )
		{
			jwtClaims[ claim.Key ] = claim.Value;
		}

		return CreateClient( getJwtInfo, jwtClaims );
	}

	private HttpClient CreateClient( Func<JwtInfo> getJwtInfo, Dictionary<string, string> claims )
	{
		var client = CreateClient();

		// Can't call this Func<> until base.CreateClient() has been called which fires up the Api WebApplication and all
		// the delegates that configure the 'test environment'
		var jwtInfo = getJwtInfo();

		var bearerToken = new JwtToken(
			jwtInfo.Secret,
			jwtInfo.Issuer,
			claims,
			DateTimeService.UtcNow.AddMinutes( 2 )
		).WriteToken();

		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", bearerToken );

		return client;
	}

	public string GetAssetPath( string assetName ) => Path.Combine( Directory.GetCurrentDirectory(), "../../../Assets", assetName );

	public virtual Stream GetAssetStream( string assetName ) => File.OpenRead( GetAssetPath( assetName ) );
	public virtual long GetAssetSize( string assetName ) => new FileInfo( GetAssetPath( assetName ) ).Length;


	public JsonNode GetJsonAsset( string assetName ) 
	{
		using var fs = File.OpenRead( GetAssetPath( assetName ) );
		return JsonNode.Parse( fs )!;
	}

	public T GetJsonAsset<T>( string assetName ) 
	{		
		using var fs = File.OpenRead( GetAssetPath( assetName ) );
		return JsonSerializer.Deserialize<T>( fs, new JsonSerializerOptions { PropertyNameCaseInsensitive = true } )!;
	}

	public void AssertNotification( IEnumerable<string> to, string subject, string body ) =>
		AssertNotification( 1, to, subject, body );

	public void AssertNotification( int count, IEnumerable<string> to, string subject, string body )
	{
		EmailService.SentMessages.Should().HaveCount( count );
		if ( count > 0 )
		{
			EmailService.SentMessages[ 0 ].Subject.Should().Be( subject );
			EmailService.SentMessages[ 0 ].From.Should().Be( EmailService.DefaultFrom );
			EmailService.SentMessages[ 0 ].To.Should().Be( string.Join( ";", to.Distinct() ) );
			EmailService.SentMessages[ 0 ].Body.Should().Be( body );
		}
	}
}