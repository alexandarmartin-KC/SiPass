using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SiPassHealth.Security;
using SiPassHealth.SiPass;
using SiPassHealth.StateEngine;
using SiPassHealth.Storage;

namespace SiPassHealth.Web;

public static class OverviewEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/overview", GetOverviewAsync)
            .RequireAuthorization(Policies.Viewer);

        app.MapGet("/api/controllers", GetControllersAsync)
            .RequireAuthorization(Policies.Viewer);

        app.MapGet("/api/controllers/{id}", GetControllerAsync)
            .RequireAuthorization(Policies.Viewer);

        app.MapGet("/api/events", GetEventsAsync)
            .RequireAuthorization(Policies.Viewer);
    }

    private static async Task<IResult> GetOverviewAsync(AppDbContext db, SipassConnectionState connection)
    {
        var snapshots = await db.StatusSnapshots.AsNoTracking().ToListAsync();
        var objects = await db.Objects.AsNoTracking().ToListAsync();
        var counts = snapshots.GroupBy(x => x.DerivedState).ToDictionary(x => x.Key.ToString(), x => x.Count());
        var topOffline = snapshots.Where(x => x.DerivedState == DerivedState.Offline).Take(10).ToList();
        var topUnstable = snapshots.Where(x => x.DerivedState == DerivedState.Unstable).Take(10).ToList();

        var payload = new
        {
            counts,
            topOffline,
            topUnstable,
            snapshots,
            objects,
            sipassConnection = new { connection.IsConnected }
        };

        return Results.Ok(MetaEnvelope.Wrap(payload, connection));
    }

    private static async Task<IResult> GetControllersAsync(AppDbContext db, SipassConnectionState connection)
    {
        var controllers = await db.Objects
            .Where(x => x.Type == ObjectType.Controller)
            .AsNoTracking()
            .ToListAsync();

        return Results.Ok(MetaEnvelope.Wrap(controllers, connection));
    }

    private static async Task<IResult> GetControllerAsync(string id, AppDbContext db, SipassConnectionState connection)
    {
        var controller = await db.Objects.AsNoTracking().FirstOrDefaultAsync(x => x.ObjectId == id);
        if (controller is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(MetaEnvelope.Wrap(controller, connection));
    }

    private static async Task<IResult> GetEventsAsync(AppDbContext db, SipassConnectionState connection)
    {
        var events = await db.EventLogs
            .OrderByDescending(x => x.Timestamp)
            .Take(100)
            .AsNoTracking()
            .ToListAsync();

        return Results.Ok(MetaEnvelope.Wrap(events, connection));
    }
}
