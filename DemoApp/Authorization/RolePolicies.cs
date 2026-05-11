using Microsoft.AspNetCore.Authorization;
using Models;

namespace DemoApp.Authorization;

public static class RolePolicies
{
    public const string AdminOnly = nameof(AdminOnly);
    public const string DocumentManagerArea = nameof(DocumentManagerArea);
    public const string SignerArea = nameof(SignerArea);

    public static void AddDemoRolePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, p =>
            p.RequireAssertion(ctx =>
                ctx.User.FindFirst("SystemRole")?.Value == nameof(SystemRole.SystemAdmin)));

        options.AddPolicy(DocumentManagerArea, p =>
            p.RequireAssertion(ctx =>
            {
                var r = ctx.User.FindFirst("SystemRole")?.Value;
                return r is nameof(SystemRole.SystemAdmin) or nameof(SystemRole.DocumentManager);
            }));

        options.AddPolicy(SignerArea, p =>
            p.RequireAssertion(ctx =>
            {
                var r = ctx.User.FindFirst("SystemRole")?.Value;
                return r is nameof(SystemRole.SystemAdmin)
                    or nameof(SystemRole.DocumentManager)
                    or nameof(SystemRole.Signer);
            }));
    }
}
