namespace KAT.Camelot.Abstractions.Api.Contracts.Excel.V1;

public static class ApiEndpoints
{
	private const string VersionBase = "/v1";

	public static class Utility
	{
		private const string Base = $"{VersionBase}/utility";
		public const string SpreadsheetGear = $"{Base}/ssg-license";
		public const string EmailBlast = $"{Base}/email-blast";
		public const string WaitEmailBlastComplete = $"{Base}/email-blast/wait/{{token}}";

		public static class Build
		{
			public static string WaitEmailBlastComplete( string token ) => $"{EmailBlast}/wait/{token}";
		}
	}

	public static class xDSData
	{
		private const string Base = $"{VersionBase}/xds-data";
		public const string Get = $"{Base}/{{target}}/{{group}}/{{authId}}";
		public const string GlobalTables = $"{Base}/global-tables";

		public static class Build
		{
			public static string Get( string target, string group, string authId ) => $"{Base}/{target}/{group}/{authId}";
		}
	}

	public static class CalcEngines
	{
		private const string Base = $"{VersionBase}/calc-engines";
		public const string Get = $"{Base}/{{name}}";
		public const string DebugListing = $"{Get}/debug";
		public const string DebugDownload = $"{Get}/debug/{{versionKey:int}}/download";
		public const string DownloadLatest = $"{Get}/download";
		public const string Checkout = $"{Get}/checkout";
		public const string Checkin = $"{Get}/checkin";

		public static class Build
		{
			public static string Get( string calcEngine ) => $"{Base}/{calcEngine}";
			public static string DebugListing( string calcEngine ) => $"{Base}/{calcEngine}/debug";
			public static string DebugDownload( string calcEngine, int versionKey ) => $"{Base}/{calcEngine}/debug/{versionKey}/download";
			public static string DownloadLatest( string calcEngine ) => $"{Base}/{calcEngine}/download";
			public static string Checkout( string calcEngine ) => $"{Base}/{calcEngine}/checkout";
			public static string Checkin( string calcEngine ) => $"{Base}/{calcEngine}/checkin";
		}
	}
}