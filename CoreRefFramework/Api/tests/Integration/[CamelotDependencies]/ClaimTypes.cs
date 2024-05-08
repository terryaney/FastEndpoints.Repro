namespace KAT.Camelot.Domain.Security.Identity;

public static class ClaimTypes
{
    public static class Realm
    {
        public const string Admittance = "camelot:admittance";
        public const string IsKing = "camelot:isKing";
    }
    public static class RBLe
    {
        public const string Payload = "camelot:rble:payload";
    }
}