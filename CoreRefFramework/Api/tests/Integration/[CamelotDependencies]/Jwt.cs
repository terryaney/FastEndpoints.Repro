namespace KAT.Camelot.Domain.Configuration;

public class Jwt
{    
#pragma warning disable IDE1006 // Naming rule violation

    public JwtInfo? xDS { get; set; }

#pragma warning restore IDE1006 // Naming rule violation

    public required JwtInfo WebServiceProxy { get; set; }
    
	/* UAT needs to match QA because localhost:100 is load balanced to include QA */
	public required JwtInfo RBLe { get; set; } // Used in JwtUpdate processing currently, will be same for RBL web api when implemented
    public required JwtInfo KatDataStore { get; set; }
    public JwtInfo? DataLockerSso { get; set; }
    public JwtInfo? DataLockerPropertyBag { get; set; }
    public JwtInfo? DataLocker { get; set; }
    public JwtRsaInfo? LivePerson { get; set; }
}

public class JwtInfo
{
    public required string Secret { get; set; }
    public required string Issuer { get; set; }
}

public class JwtRsaInfo
{
    public required string KeyFile { get; set; }
    public required string Issuer { get; set; }
}
