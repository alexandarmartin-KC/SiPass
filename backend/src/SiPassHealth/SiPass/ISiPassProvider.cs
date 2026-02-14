using SiPassHealth.StateEngine;

namespace SiPassHealth.SiPass;

public interface ISiPassProvider
{
    Task RenewAsync(CancellationToken cancellationToken);
    Task<SiPassSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}

public sealed record SiPassSnapshot(
    bool IsConnected,
    bool DataStale,
    DateTimeOffset SnapshotAt,
    IReadOnlyList<SiPassDeviceSnapshot> Devices);

public sealed record SiPassDeviceSnapshot(
    string ObjectId,
    ObjectType Type,
    string Name,
    string Path,
    string? ParentId,
    bool IsOnline,
    DateTimeOffset SeenAt);
