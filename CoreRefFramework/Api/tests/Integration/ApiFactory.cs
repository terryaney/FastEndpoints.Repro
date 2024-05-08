using Xunit;
using KAT.Camelot.Testing.Integration;
using System.Text.Json.Nodes;
using KAT.Camelot.Api.Excel;

namespace Camelot.Api.Excel.Tests.Integration;

public class ApiFactory: BaseFactory<IApiMarker>, IAsyncLifetime
{
	private readonly MockServer mockServer = new();

	public new HttpClient CreateClient() => CreateClient( () => jwt.WebServiceProxy );

	protected override void ConfigureTestServices( IServiceCollection services )
	{
		// Replace QA web services with WireMock servers...
		services.AddHttpClient(
			"DataLocker",
			httpClient =>
			{
				httpClient.BaseAddress = new Uri( mockServer.Url );
				httpClient.DefaultRequestHeaders.Add( "Accept", "*/*" );
				httpClient.DefaultRequestHeaders.Add( "WireMock", "true" );
			}
		);
		services.AddHttpClient(
			"xDS",
			httpClient =>
			{
				httpClient.BaseAddress = new Uri( mockServer.Url );
				httpClient.DefaultRequestHeaders.Add( "WireMock", "true" );
			}
		);
	}

	public JsonNode GetMockServerJsonAsset( string assetName ) 
	{
		using var fs = File.OpenRead( GetAssetPath( Path.Combine( "MockServer", assetName ) ) );
		return JsonNode.Parse( fs )!;
	}

	public Task InitializeAsync() 
	{
		mockServer.Start();
		return Task.CompletedTask;
	}

	public new Task DisposeAsync()
	{
		mockServer.Dispose();
		return Task.CompletedTask;
	}
}