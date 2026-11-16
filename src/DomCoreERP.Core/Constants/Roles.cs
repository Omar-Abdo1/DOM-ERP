namespace DomCoreERP.Core.Constants;

public static class Roles
{
    public const string SuperAdmin    = "SuperAdmin";
    public const string CompanyAdmin  = "CompanyAdmin";
    public const string Employee      = "Employee";

    public static readonly string[] All = { SuperAdmin, CompanyAdmin, Employee };
}