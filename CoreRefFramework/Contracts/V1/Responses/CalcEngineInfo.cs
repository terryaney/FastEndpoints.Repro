namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1.Responses;

public class CalcEngineInfo
{
	public string? CheckedOutBy { get; init; }
	public required double Version { get; init; }
}