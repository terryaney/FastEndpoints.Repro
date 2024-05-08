namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Responses;

public class DebugFile
{
	/// <summary>
	/// The data store VersionKey of the debug CalcEngine.
	/// </summary>
	public required int VersionKey { get; init; }

	/// <summary>
	/// The AuthId of the participant ran in the debug CalcEngine.
	/// </summary>
	public required string AuthId { get; init; }
	/// <summary>
	/// The date the debug CalcEngine was uploaded.
	/// </summary>
	public required DateTime DateUploaded { get; init; }
}