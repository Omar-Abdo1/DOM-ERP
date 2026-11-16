public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Role { get; }
    Guid? CompanyId { get; }
    bool IsSuperAdmin { get; }
    Guid? ResolvedCompanyId { get; }
}