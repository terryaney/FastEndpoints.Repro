using FluentAssertions;
using FluentAssertions.Json;
using Contracts = KAT.Camelot.Abstractions.Api.Contracts.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xunit;
using System.Text.Json.Nodes;
using KAT.Camelot.Testing.Integration;

namespace Camelot.Api.Excel.Tests.Integration.UtilityEndpointTests;

public class GetTests : IClassFixture<ApiFactory>
{
	private readonly ApiFactory factory;
	private readonly HttpClient client;
	public GetTests( ApiFactory factory )
	{
		this.factory = factory;
		client = factory.CreateClient();
	}

	[Fact]
	public async Task Get_ReturnsValidation_WhenParametersMissing()
	{
		// Arrange
		await Task.Delay( 1 );

		// Act

		// Assert 
	}
}