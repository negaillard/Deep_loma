using System.Security.Claims;
using Models.Enums;

namespace DemoApp.Helpers;

public static class DemoHomeRoutes
{
    public static string ForRole(SystemRole role) => role switch
    {
        SystemRole.SystemAdmin => "/Roles",
        SystemRole.DocumentManager => "/Documents",
        SystemRole.Signer => "/Sign",
        _ => "/Account/Login"
    };

    public static string FromClaims(ClaimsPrincipal user)
    {
        var s = user.FindFirst("SystemRole")?.Value;
        if (Enum.TryParse<SystemRole>(s, out var role))
            return ForRole(role);
        return "/Account/Login";
    }
}
