namespace DomCoreERP.Core.Config;

public class SeedSettings
{
    public SeedAdminSettings SuperAdmin { get; set; } = new();
    public string DemoPassword { get; set; } = "Demo!123";
}

public class SeedAdminSettings
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FullName { get; set; } = null!;
}