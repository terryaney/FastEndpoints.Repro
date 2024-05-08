using WireMock.Server;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Contracts = KAT.Camelot.Abstractions.Api.Contracts.Excel;
using System.Net;

namespace Camelot.Api.Excel.Tests.Integration;

public class MockServer : IDisposable
{
	private WireMockServer server = default!;
    
	public string Url => server.Url!;
	
	public void Start()
    {
		server = WireMockServer.Start();
		SetupGets();
	}

    void SetupGets()
    {
		// This never really is hit, was trying to match missing authorization header, but couldn't figure out a 
		// clean way to simulate this test because the api that WireMock is replacing is the one that sets these
		// credentials, so by using WireMock, no one is going to 'mess up' the credentials.  Just so that I can
		// test the invalid response (so I can code gracefully to handle), I'll make a new url that no one would
		// use except for my test project just to get the response back and handle gracefully
		/*
		server.Given(
			Request.Create()
				.WithHeader( 
					"Basic", 
					Convert.ToBase64String( Encoding.UTF8.GetBytes( "user:pwd" ) ), 
					WireMock.Matchers.MatchBehaviour.RejectOnMatch 
				)
				.UsingGet()				
		).RespondWith(
			Response.Create()
				.WithHeader( "nexgen-authorization-failed", "true" )
				.WithStatusCode( HttpStatusCode.Unauthorized )
				.WithBody( 
"""
{
	"timestamp": 1676572342225,
	"status": 401,
	"error": "Unauthorized",
	"path": "{{request.path}}"
}
"""
				)
		);
		*/

		var getRequests = new[]
		{
			new RequestInfo { 
				Url = Contracts.V1.ApiEndpoints.xDSData.Build.Get( "EW.QA", "AZI", "111111111" ), 
				ResponseFile = "xDSResponses/ProfileOnly.json",
			}
		};

		foreach( var r in getRequests )
		{
			var request = Request.Create().WithUrl( $"{Url}{r.Url}" );

			if ( r.Method == HttpMethod.Get )
			{
				request.UsingGet();
			}
			else
			{
				request.UsingPost();
			}

			if ( r.HeaderValues != null )
			{
				foreach( var k in r.HeaderValues.Keys )
				{
					request.WithHeader( k, r.HeaderValues[ k ], WireMock.Matchers.MatchBehaviour.AcceptOnMatch );
				}
			}

			if ( r.BodyValues != null )
			{
				request.WithBody( values => {
					foreach( var k in r.BodyValues.Keys )
					{
						if ( values == null || !values.TryGetValue( k, out var value ) || WebUtility.UrlDecode( value ) != r.BodyValues[ k ] )
						{
							return false;
						}
					}

					return true;
				} );
			}

			var response = Response.Create()
				.WithHeader( "content-type", r.ContentType )
				.WithStatusCode( HttpStatusCode.OK );

			response.WithBodyFromFile( Path.Combine( Directory.GetCurrentDirectory(), "../../../Assets/MockServer", r.ResponseFile ) );

			server.Given( request ).RespondWith( response );
		}
	}

	class RequestInfo
	{
		public HttpMethod Method { get; init; } = HttpMethod.Get;
		public required string Url { get; init; }
		public string ContentType { get; init; } = "application/json; charset=utf-8";
		public required string ResponseFile { get; init; }
		public Dictionary<string, string>? HeaderValues { get; init; }
		public Dictionary<string, string>? BodyValues { get; init; }
	}

	public void Dispose() 
    {
		server.Stop();
		server.Dispose();
	}
}