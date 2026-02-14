using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using SiPassHealth.Security;

namespace SiPassHealth.Web;

public static class SseEndpoint
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/api/stream", StreamAsync).RequireAuthorization(Policies.Viewer);
    }

    private static async Task StreamAsync(HttpContext context, SseBroker broker, CancellationToken cancellationToken)
    {
        context.Response.Headers["Content-Type"] = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.Headers["Connection"] = "keep-alive";

        await foreach (var item in broker.Subscribe(cancellationToken))
        {
            var json = JsonSerializer.Serialize(item.Payload);
            await context.Response.WriteAsync($"event: {item.Event}\n");
            await context.Response.WriteAsync($"data: {json}\n\n");
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
}

public sealed class SseBroker
{
    private readonly Channel<SseItem> _channel = Channel.CreateUnbounded<SseItem>();

    public IAsyncEnumerable<SseItem> Subscribe(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public void Publish(string eventName, object payload)
    {
        _channel.Writer.TryWrite(new SseItem(eventName, payload));
    }
}

public sealed record SseItem(string Event, object Payload);
