using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Infrastructure.Services;

/// <summary>
/// Reads the authenticated user's ID from the JWT claims via IHttpContextAccessor.
/// Returns null if called from an unauthenticated context (e.g. the auth endpoints themselves).
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            string? value = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }
}
