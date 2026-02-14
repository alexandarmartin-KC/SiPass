using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SiPassHealth.Storage;

namespace SiPassHealth.Security;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/login", LoginAsync)
            .RequireAntiforgeryToken()
            .AllowAnonymous();
        app.MapPost("/api/auth/logout", LogoutAsync)
            .RequireAntiforgeryToken()
            .RequireAuthorization(Policies.Viewer);
        app.MapGet("/api/auth/me", MeAsync).RequireAuthorization(Policies.Viewer);
        app.MapGet("/api/auth/csrf", CsrfAsync).AllowAnonymous();
        app.MapGet("/api/auth/denied", () => Results.Forbid()).AllowAnonymous();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signInManager,
        HttpContext httpContext,
        AppDbContext db,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("Auth");
        var user = await users.FindByNameAsync(request.Username);
        if (user is null)
        {
            await AuditLog.WriteAsync(db, "Warning", null, "Login failed", "Auth", httpContext.TraceIdentifier);
            return Results.Unauthorized();
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, true, true);
        if (!result.Succeeded)
        {
            await AuditLog.WriteAsync(db, "Warning", user.Id, "Login failed", "Auth", httpContext.TraceIdentifier);
            return Results.Unauthorized();
        }

        await AuditLog.WriteAsync(db, "Info", user.Id, "Login success", "Auth", httpContext.TraceIdentifier);
        return Results.Ok();
    }

    private static async Task<IResult> LogoutAsync(HttpContext context, AppDbContext db)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await AuditLog.WriteAsync(db, "Info", null, "Logout", "Auth", context.TraceIdentifier);
        return Results.Ok();
    }

    private static async Task<IResult> MeAsync(UserManager<ApplicationUser> users, HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var user = await users.GetUserAsync(context.User);
        var roles = user is null ? Array.Empty<string>() : await users.GetRolesAsync(user);
        return Results.Ok(new
        {
            username = user?.UserName,
            roles
        });
    }

    private static IResult CsrfAsync(IAntiforgery antiforgery, HttpContext context)
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Ok(new { token = tokens.RequestToken });
    }

    private record LoginRequest(string Username, string Password);
}
