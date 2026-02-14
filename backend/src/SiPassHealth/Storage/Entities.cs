using SiPassHealth.StateEngine;

namespace SiPassHealth.Storage;

public sealed class ObjectEntity
{
    public string ObjectId { get; set; } = string.Empty;
    public ObjectType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string Path { get; set; } = string.Empty;
}

public sealed class StatusSnapshot
{
    public string ObjectId { get; set; } = string.Empty;
    public bool RawComms { get; set; }
    public DerivedState DerivedState { get; set; }
    public DateTimeOffset? OfflineSince { get; set; }
    public DateTimeOffset? LastOnlineAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public int FlapCount24h { get; set; }
    public int TotalOfflineSeconds24h { get; set; }
}

public sealed class EventLog
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? ObjectId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

public static class AuditLog
{
    public static async Task WriteAsync(AppDbContext db, string severity, string? userId, string message, string source, string correlationId)
    {
        var entry = new EventLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            Severity = severity,
            ObjectId = null,
            Message = message,
            Source = source,
            CorrelationId = correlationId,
            UserId = userId
        };

        db.EventLogs.Add(entry);
        await db.SaveChangesAsync();
    }
}
