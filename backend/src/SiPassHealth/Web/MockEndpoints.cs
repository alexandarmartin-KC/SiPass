using Microsoft.AspNetCore.Authorization;
using SiPassHealth.Security;
using SiPassHealth.SiPass.Mock;

namespace SiPassHealth.Web;

public static class MockEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/mock/scenario", GetScenarioAsync)
            .RequireAuthorization(Policies.Admin);

        app.MapPost("/api/mock/scenario", SetScenarioAsync)
            .RequireAuthorization(Policies.Admin);
    }

    private static Task<IResult> GetScenarioAsync(IMockScenarioControl? control)
    {
        if (control is null)
        {
            return Task.FromResult(Results.Conflict(new { message = "Mock mode is not enabled." }));
        }

        return Task.FromResult<IResult>(Results.Ok(new
        {
            scenario = control.Scenario,
            available = control.Scenarios
        }));
    }

    private static Task<IResult> SetScenarioAsync(ScenarioRequest request, IMockScenarioControl? control)
    {
        if (control is null)
        {
            return Task.FromResult(Results.Conflict(new { message = "Mock mode is not enabled." }));
        }

        try
        {
            control.SetScenario(request.Scenario ?? string.Empty);
        }
        catch (ArgumentException)
        {
            return Task.FromResult(Results.BadRequest(new { message = "Unknown scenario." }));
        }

        return Task.FromResult<IResult>(Results.Ok(new { scenario = control.Scenario }));
    }

    private sealed record ScenarioRequest(string? Scenario);
}
