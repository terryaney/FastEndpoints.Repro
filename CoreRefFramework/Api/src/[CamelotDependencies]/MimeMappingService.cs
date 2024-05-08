using KAT.Camelot.Domain.Services;
using Microsoft.AspNetCore.StaticFiles;

namespace KAT.Camelot.Infrastructure.Web.Services;

// See BTR.Camelot.Api.xDS.IntegrationTests.Fakes from original to see why we have this
// https://dotnetcoretutorials.com/2018/08/14/getting-a-mime-type-from-a-file-name-in-net-core/
public class MimeMappingService : IMimeMappingService
{
	private readonly FileExtensionContentTypeProvider _contentTypeProvider;

	public MimeMappingService( FileExtensionContentTypeProvider contentTypeProvider )
	{
		_contentTypeProvider = contentTypeProvider;
	}

	public string Map( string fileName )
	{
		if ( !_contentTypeProvider.TryGetContentType( fileName, out var contentType ) )
		{
			contentType = "application/octet-stream";
		}
		return contentType;
	}
}
