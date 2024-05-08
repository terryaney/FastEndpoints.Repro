namespace KAT.Camelot.Domain.Http;

public static class ApiResponseHeaders
{
	public static class Jwt
	{
		public const string Expired = "camelot-jwt-expired";

		public static class Failed
		{
			public const string Header = "camelot-jwt-failed";
			public const string Detail = "camelot-jwt-failed-detail";
			public const string Payload = "camelot-jwt-failed-payload";
			public const string Dates = "camelot-jwt-failed-dates";
		}
	}

	public static class DataLocker
	{
		public const string FilesDeleted = "camelot-deleted-files";
		public const string VersionsDeleted = "camelot-deleted-versions";
		public const string FileVersion = "camelot-version";
	}
}