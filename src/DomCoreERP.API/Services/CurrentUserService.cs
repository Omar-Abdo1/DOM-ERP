using System.Security.Claims;
using DomCoreERP.Core.Constants;
using Microsoft.AspNetCore.Http;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private bool IsSystemContext => _http.HttpContext is null;

    public Guid? UserId =>
        Guid.TryParse(
            _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out var id)
            ? id : null;

    public string? Role =>
        _http.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

    public Guid? CompanyId =>
        Guid.TryParse(
            _http.HttpContext?.User.FindFirst("companyId")?.Value,
            out var id)
            ? id : null;

    public bool IsSuperAdmin =>
        IsSystemContext ||
        (_http.HttpContext?.User.IsInRole(Roles.SuperAdmin) ?? false);   // ← uses constant + IsInRole

    public Guid? ResolvedCompanyId
    {
        get
        {
            if (IsSystemContext) return null;

            if (IsSuperAdmin)
            {
                var headerValue = _http.HttpContext!.Request
                    .Headers["X-Company-Id"].FirstOrDefault();
                return Guid.TryParse(headerValue, out var companyId) ? companyId : null;
            }

            return CompanyId;
        }
    }
}