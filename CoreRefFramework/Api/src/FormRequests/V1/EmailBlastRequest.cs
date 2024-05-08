using FluentValidation;
using KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;

namespace KAT.Camelot.Api.Excel.FormRequests.V1;

public class FileUploadRequest : KatCredentialRequest
{
	public required IFormFile File { get; init; }
}

public class FileUploadRequestValidator : AbstractValidator<FileUploadRequest>
{
	public FileUploadRequestValidator()
	{
		var validDocumentTypes = new[] { ".zip" };

		RuleFor( r => r.File )
			.NotEmpty()
			.Must( f => 
				!string.IsNullOrEmpty( f.FileName ) && 
				f.Length > 0 && f.Length < ( 10 * 1024 * 1024 ) &&
				validDocumentTypes.Any( t => string.Compare( Path.GetExtension( f.FileName ), t, true ) == 0 )
			)
			.WithMessage( $"'{{PropertyName}}' must be provided (with a file name) and be less than 10MB in size.  The following file types are supported:  {string.Join( ", ", validDocumentTypes.Select( ( e, i ) => $"{( i == validDocumentTypes.Length - 1 ? "or " : "" )}{e[ 1.. ].ToUpper()}" ) )}." );
	}
}