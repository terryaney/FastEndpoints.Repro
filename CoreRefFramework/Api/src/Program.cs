using KAT.Camelot.Api.Excel;
using KAT.Camelot.Abstractions.Api.Contracts.Excel;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder( args );

builder.AddApiConfiguration<IApiMarker, IContractsMarker>( options =>
{
	options.ApiName = "Camelot Excel";

	options.TagDescriptions = t =>
	{
		t[ "Utility" ] = "Utility endpoints to perform miscellaneous tasks required my Excel Add-In.";
	};

	options.DocumentSettings = s =>
	{
		s.DocumentName = "Release 1.0";
		s.Title = "KAT Camelot Excel API";
		s.Version = "v1.0";
	};
} );

builder.Services
	.RemoveAll<IDistributedCache>()
	.AddDistributedMemoryCache();

var app = builder.Build();

app.UseApiConfiguration<IApiMarker>( options => {
	options.ApiName = "Camelot Api Excel";
	options.UseAuthentication = false;
} );

app.Run();
