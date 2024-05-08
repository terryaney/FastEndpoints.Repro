using Contracts = KAT.Camelot.Abstractions.Api.Contracts.Excel;
using KAT.Camelot.Api.Excel.FormRequests.V1;
using System.IO.Compression;
using System.Text.Json;
using KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Requests;
using System.Xml.Linq;
using System.Text.Json.Nodes;
using HF = Hangfire;

namespace KAT.Camelot.Api.Excel.Features.Utility.EmailBlast;

public partial class Endpoint : CamelotEndpoint<FileUploadRequest, string>
{
	public Endpoint() : base() { }

	public override void Configure()
	{
		Post( Contracts.V1.ApiEndpoints.Utility.EmailBlast[ 3.. ] );
		AllowFileUploads();
		AllowAnonymous();
	}

	public override async Task HandleAsync( FileUploadRequest r, CancellationToken c )
	{
		var authId = "normallyLookUpInDatabase";

		if ( authId == null )
		{
			return;
		}

		using var zipFile = new ZipArchive( r.File.OpenReadStream() );
		
		using var fsConfig = zipFile.Entries.Single( e => e.Name == ".configuration.json" ).Open();
		using var fsContent = zipFile.Entries.Single( e => e.Name == ".content.html" ).Open();

		var request = ( await JsonSerializer.DeserializeAsync<EmailBlastRequest>( fsConfig, cancellationToken: c ) )!;
		var content = await new StreamReader( fsContent ).ReadToEndAsync( c );

		var validator = new EmailBlastRequestValidator();
		var validationResult = await validator.ValidateAsync( request, cancellation: c );

		if ( !validationResult.IsValid )
		{
			foreach ( var error in validationResult.Errors )
			{
				AddError( error );
			}

			ThrowIfAnyErrors();
		}

		var attachments = await Task.WhenAll(
			request.Attachments.Select( async a =>
			{
				using var fs = zipFile.Entries.Single( e => e.Name == a.Id ).Open();
				using var ms = new MemoryStream();
				await fs.CopyToAsync( ms, c );

				var fileContent = Convert.ToBase64String( ms.ToArray() );

				return new XElement( "attachment",
					new XAttribute( "fileName", a.Name ),
					!string.IsNullOrEmpty( a.ContentId ) ? new XAttribute( "contentId", a.ContentId ) : null,
					fileContent
				);
			} )
		);

		var inputPackage =
			new XElement( "InputPackage",
				new XElement( "Invoker",
					new XAttribute( "Assembly", "BTR.Evolution.Hangfire" /* invokeType.Assembly.FullName.Split( ',' )[ 0 ] */ ),
					new XAttribute( "Type", "BTR.Evolution.Hangfire.Jobs.EmailBlaster" /* invokeType.FullName */ )
				),
				new XElement( "blast", 
					new XAttribute( "batchSize", request.AddressesPerEmail ),
					new XAttribute( "batchWaitMinutes", request.WaitPerBatch ),
					!string.IsNullOrEmpty( request.Bcc ) ? new XAttribute( "bcc", request.Bcc ) : null,
					!string.IsNullOrEmpty( request.Audit ) ? new XAttribute( "audit", request.Audit ) : null,

					new XElement( "from", request.From ),

					new XElement( "attachments", attachments ),

					new XElement( "body",
						new XAttribute( "subject", request.Subject ),
						content
					),

					new XElement( "recipients",
						request.Recipients.Cast<JsonObject>().Select( r =>
							new XElement( "r", 
								r.Select( c => c.Value != null && c.Key != "c0" ? new XAttribute( c.Key, (string)c.Value! ) : null ),
								(string)r[ "c0" ]!
							)
						)
					)
				)
			);

		var hangfireConnection = "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=SSPI;Initial Catalog=HangfireDB;";
		HF.JobStorage.Current = new HF.SqlServer.SqlServerStorage( hangfireConnection );

		// Comment out call to Hangfire and SendStringAsync to test the endpoint without Hangfire/.NET Framework
		var hangfireId = HF.BackgroundJob.Enqueue( () => 
			new BTR.Evolution.Hangfire.Schedulers.JobInvoker().Invoke(
				$"Email Blast From {authId}",
				inputPackage.ToString(),
				null,
				HF.JobCancellationToken.Null 
			)
		);
		await SendStringAsync( hangfireId, cancellation: c );

		// await SendStringAsync( "1234", cancellation: c );
	}
}