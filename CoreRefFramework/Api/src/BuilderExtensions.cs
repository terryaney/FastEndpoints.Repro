using System.Diagnostics;
using System.Text.Json.Serialization;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSwag.Generation.AspNetCore;
using KAT.Camelot.Domain.Http;
using KAT.Camelot.Domain.Services;
using KAT.Camelot.Domain.Web.Extensions;
using KAT.Camelot.Domain.Web.Http;
using KAT.Camelot.Infrastructure.Services;
using KAT.Camelot.Infrastructure.Web.Services;

namespace KAT.Camelot.Api.Excel;

public class AddApiConfigurationOptions : BaseApiConfigurationOptions
{
	public Dictionary<string, string[]> JwtTypes { get; set; } = new();
	public Dictionary<string, string[]>? AlternateJwtTypes { get; set; }
	public Dictionary<string, string> FileExtensionMappings { get; set; } = new();
	public Action<Dictionary<string, string>> TagDescriptions { get; set; } = t => { };
	public Action<AspNetCoreOpenApiDocumentGeneratorSettings> DocumentSettings { get; set; } = s => { };
	public Action<AuthorizationOptions>? AddAuthorization { get; set; }
	public Action<JwtBearerEvents>? ConfigureJwtBearerEvents { get; set; }
	public Action<IHealthChecksBuilder>? AddHealthChecks { get; set; }
}

public class UseApiConfigurationOptions : BaseApiConfigurationOptions
{
	public Func<HttpContext, Exception, Dictionary<string, object?>?>? GetProblemDetailsExtensions { get; set; }
}

public abstract class BaseApiConfigurationOptions
{
	public string ApiName { get; set; } = null!;
	public bool UseCors { get; set; }
	public bool UseAuthentication { get; set; } = true;
}

public class JwtBearerEvents
{
	//
	// Summary:
	//     Invoked if authentication fails during request processing. The exceptions will
	//     be re-thrown after this event unless suppressed.
	public Func<AuthenticationFailedContext, Task>? OnAuthenticationFailed { get; set; }
	//
	// Summary:
	//     Invoked if Authorization fails and results in a Forbidden response.
	public Func<ForbiddenContext, Task>? OnForbidden { get; set; }
	//
	// Summary:
	//     Invoked when a protocol message is first received.
	public Func<MessageReceivedContext, Task>? OnMessageReceived { get; set; }
	//
	// Summary:
	//     Invoked after the security token has passed validation and a ClaimsIdentity has
	//     been generated.
	public Func<TokenValidatedContext, Task>? OnTokenValidated { get; set; }
	//
	// Summary:
	//     Invoked before a challenge is sent back to the caller.
	public Func<JwtBearerChallengeContext, Task>? OnChallenge { get; set; }
}

public static partial class ConfigurationExtensions
{
	public static WebApplicationBuilder AddApiConfiguration<TLogger, TContracts>(
		this WebApplicationBuilder builder,
		Action<AddApiConfigurationOptions>? options = null
	)
	{
		var opts = new AddApiConfigurationOptions();
		options?.Invoke( opts );

		FluentValidation.ValidatorOptions.Global.DefaultRuleLevelCascadeMode = FluentValidation.CascadeMode.Stop;

		if ( opts.UseCors )
		{
			builder.Services.AddCors( options =>
			{
				options.AddDefaultPolicy( builder =>
				{
					builder.AllowAnyOrigin();
					builder.WithMethods( "GET", "OPTIONS" );
					builder.WithHeaders( "Content-Type", "If-Modified-Since", "Cache-Control" );
				} );
			} );
		}

		var healthChecksBuilder = builder.Services.AddHealthChecks();
		opts.AddHealthChecks?.Invoke( healthChecksBuilder );

		builder.Services.AddFastEndpoints( options =>
		{
			options.Assemblies = new[] { typeof( TLogger ).Assembly, typeof( TContracts ).Assembly };
			options.IncludeAbstractValidators = true;
		} );

		builder.Services.SwaggerDocument( options =>
		{
			options.RemoveEmptyRequestSchema = true;
			options.AutoTagPathSegmentIndex = 0;
			options.TagDescriptions = opts.TagDescriptions;
			options.MaxEndpointVersion = 1;

			// options.ShortSchemaNames = true;
			void schemaNameSettings( AspNetCoreOpenApiDocumentGeneratorSettings s )
			{
				s.SchemaNameGenerator = new ContractsNameGenerator();
				s.OperationProcessors.Add( new AddResponseHeadersProcessor() );
				opts.DocumentSettings( s );
			}
			options.DocumentSettings = schemaNameSettings;
		} );

		builder.Services
			.AddScoped<IDistributedCache, NullDistributedCache>()
			.AddHttpClient()
			.AddScoped<IDateTimeService, DateTimeService>()
			.AddScoped<IEmailService, EmailService>()
			.AddScoped<ITextService, TextService>()
			.AddSingleton<IMimeMappingService, MimeMappingService>( sp =>
			{
				var provider = new FileExtensionContentTypeProvider();
				foreach ( var map in opts.FileExtensionMappings.Keys )
				{
					provider.Mappings.Add( map, opts.FileExtensionMappings[ map ] );
				}
				return new MimeMappingService( provider );
			} );


		void bearerOptions( JwtBearerOptions o )
		{
			configureJwtBearerEvents( o.Events ??= new() );

			// Was setting this to TimeSpan.Zero, but this post seems to say it should be 1 min...not sure why Zero was being used
			// https://github.com/GoogleCloudPlatform/esp-v2/issues/369
			o.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes( 3 );
			// options.ValidateLifetime = false; // This seems to be problem for India developers...they get unauthorized
		}

		void configureJwtBearerEvents( Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents events )
		{
			var jwtEvents = new JwtBearerEvents();
			opts.ConfigureJwtBearerEvents?.Invoke( jwtEvents );

			events.OnTokenValidated = jwtEvents.OnTokenValidated ?? events.OnTokenValidated;
			events.OnForbidden = jwtEvents.OnForbidden ?? events.OnForbidden;
			events.OnMessageReceived = jwtEvents.OnMessageReceived ?? events.OnMessageReceived;

			events.OnChallenge = jwtEvents.OnChallenge ?? ( context =>
			{
				var detail = string.IsNullOrEmpty( context.Error ) ? "No Bearer passed" : $"{context.Error}: {context.ErrorDescription}  Exception type: {context.AuthenticateFailure?.GetType()}";

				var logger = context.HttpContext.Resolve<ILogger<TLogger>>();
				LogWarningJwtChallengeFailed( logger, opts.ApiName, context.HttpContext.Request.Method, context.HttpContext.Request.Path, detail );

				// https://stackoverflow.com/a/48736096/166231 - if I want to add content to response

				context.Response.Headers.Add( ApiResponseHeaders.Jwt.Failed.Header, "true" ); // used for logging, check original implementation
				context.Response.Headers.Add( ApiResponseHeaders.Jwt.Failed.Payload, context.Request.Headers.Authorization.ToString() );
				context.Response.Headers.Add( ApiResponseHeaders.Jwt.Failed.Detail, detail );

				try
				{
					var th = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
					var token = th.ReadJwtToken( context.Request.Headers.Authorization.ToString()[ "Bearer ".Length.. ] );
					context.Response.Headers.Append( ApiResponseHeaders.Jwt.Failed.Dates, $"Server Time: {DateTime.UtcNow:o}, Valid From: {token.ValidFrom:o}, Valid To: {token.ValidTo:o}" );
					context.Response.Headers.Append( ApiResponseHeaders.Jwt.Failed.Payload, token.ToString() );
				}
				catch
				{
					context.Response.Headers.Append( ApiResponseHeaders.Jwt.Failed.Payload, context.Request.Headers.Authorization.ToString() );
				}

				return Task.CompletedTask;
			} );

			events.OnAuthenticationFailed = jwtEvents.OnAuthenticationFailed ?? ( context =>
			{
				var logger = context.HttpContext.Resolve<ILogger<TLogger>>();
				LogWarningJwtAuthenticationFailed( logger, opts.ApiName, context.HttpContext.Request.Method, context.HttpContext.Request.Path, context.Exception.Message );

				if ( context.Exception.GetType() == typeof( SecurityTokenExpiredException ) )
				{
					context.Response.Headers.Add( ApiResponseHeaders.Jwt.Expired, "true" );
				}
				return Task.CompletedTask;
			} );
		}

		if ( opts.JwtTypes.Any() )
		{
			// https://devblogs.microsoft.com/aspnet/jwt-validation-and-authorization-in-asp-net-core/
			builder.Services.AddAuthenticationJwtBearer(
				signingOptions: o =>
				{
					// Need to assign key to non null so AddAuthenticationJwtBearer adds a signing key
					o.SigningKey = "secretUpdatedViaUpdatableJwtBearerOptions";
					o.SigningStyle = JWTBearer.TokenSigningStyle.Symmetric;
				},
				bearerOptions: o =>
				{
					bearerOptions( o );

					// https://discord.com/channels/933662816458645504/1200521676912332861/1200648920435544144
					// https://github.com/FastEndpoints/FastEndpoints/issues/526 
					// 	PR explaining reason FastEndpoints changing functionality to set to false, but I don't use FastEndpoint's `JWTBearer.CreateToken` so had a mismatch after update
					o.MapInboundClaims = true;
				} );

			// Changed this to singleton since I'm not using TheKeep anymore as a service and
			// FastEndpoints had a concern: https://discord.com/channels/933662816458645504/1200521676912332861/1200650241305755780
			builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>>( provider =>
			{
				var configuration = provider.GetRequiredService<IConfiguration>();
				var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
				var options = new UpdatableJwtBearerOptions( configuration, httpContextAccessor, opts.JwtTypes, opts.AlternateJwtTypes );
				return options;
			} );
		}

		if ( opts.AddAuthorization != null )
		{
			builder.Services
				.AddAuthorization( opts.AddAuthorization )
				.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationForbiddenHeaderHandler>();
		}

		builder.Services.Configure<RouteOptions>( options =>
		{
			options.ConstraintMap.Add( "excludevalues", typeof( ExcludeValuesConstraint ) );
		} );

		return builder;
	}

	public static IApplicationBuilder UseApiConfiguration<TLogger>( this IApplicationBuilder app, Action<UseApiConfigurationOptions>? options = null )
	{
		var opts = new UseApiConfigurationOptions();
		options?.Invoke( opts );

		if ( opts.UseCors )
		{
			app.UseCors();
		}

		if ( opts.UseAuthentication )
		{
			app.UseAuthentication();
			app.UseAuthorization();
		}

		app.UseExceptionHandler( builder =>
		{
			builder.Run( async ctx =>
			{
				var exHandlerFeature = ctx.Features.Get<IExceptionHandlerFeature>()!;
				var ex = exHandlerFeature.Error;

				var problemDetails = ex.GetProblemDetails( ctx, opts.GetProblemDetailsExtensions );

				// Conduent proxy intercepts anything that isn't 400...
				ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
				ctx.Response.ContentType = "application/problem+json";

				await ctx.Response.WriteAsJsonAsync( problemDetails );
			} );
		} );

		app.UseFastEndpoints( options =>
		{
			options.Serializer.Options.Converters.Add( new JsonStringEnumConverter() );
			options.Serializer.Options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

			options.Versioning.PrependToRoute = true;
			options.Versioning.DefaultVersion = 1;

			options.Errors.ResponseBuilder = ( failures, context, statusCode ) =>
			{
				var validationProblems =
					failures
						.GroupBy( f => f.PropertyName )
						.ToDictionary( e => e.Key, v => v.Select( m => m.ErrorMessage ).ToArray() );

				var logger = context.Resolve<ILogger<TLogger>>();
				LogWarningRequestValidations( logger, opts.ApiName, context.Request.Method, context.Request.Path, validationProblems.SelectMany( i => i.Value.Select( m => $"{i.Key}: {m}" ) ).ToArray() );

				return new ValidationProblemDetails( validationProblems )
				{
					Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
					Title = "One or more validation errors occurred.",
					Status = statusCode,
					Instance = context.Request.Path,
					Extensions =
					{
						{ "traceId", Activity.Current?.TraceId.ToString() },
						{ "spanId", Activity.Current?.SpanId.ToString() },
						{ "requestId", context.TraceIdentifier }
					}
				};
			};
		} );

		app.UseSwaggerGen();

		return app;
	}

	[LoggerMessage( 1, LogLevel.Warning, "{callerName} {method} {path} JWT OnChallenge failed: {message}" )]
	static partial void LogWarningJwtChallengeFailed( ILogger logger, string callerName, string method, string path, string message );
	[LoggerMessage( 2, LogLevel.Warning, "{callerName} {method} {path} JWT OnAuthenticationFailed failed: {message}" )]
	static partial void LogWarningJwtAuthenticationFailed( ILogger logger, string callerName, string method, string path, string message );
	[LoggerMessage( 3, LogLevel.Warning, "{callerName} {method} {path} validation errors.\n\n{validationProblems}" )]
	static partial void LogWarningRequestValidations( ILogger logger, string callerName, string method, string path, string[] validationProblems );
}