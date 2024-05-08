using Xunit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

[TestClass]
public class GetMSTests
{
	private readonly ApiFactory factory;
	public GetMSTests( ApiFactory factory )
	{
		this.factory = factory;
	}

	[TestMethod]
	public async Task Get_ReturnsValidation_WhenParametersMissing_MSTest()
	{
		// Arrange
		await Task.Delay(1);

		// Act

		// Assert 
	}
}