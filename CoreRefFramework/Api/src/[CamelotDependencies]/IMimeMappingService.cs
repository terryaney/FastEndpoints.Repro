namespace KAT.Camelot.Domain.Services;

// See BTR.Camelot.Api.xDS.IntegrationTests.Fakes from original to see why we have this
// https://dotnetcoretutorials.com/2018/08/14/getting-a-mime-type-from-a-file-name-in-net-core/
public interface IMimeMappingService
{
	string Map( string fileName );
}