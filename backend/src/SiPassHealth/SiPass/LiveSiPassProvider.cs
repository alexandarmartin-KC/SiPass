namespace SiPassHealth.SiPass;

public sealed class LiveSiPassProvider : ISiPassProvider
{
    private readonly SiPassClient _client;

    public LiveSiPassProvider(SiPassClient client)
    {
        _client = client;
    }

    public Task RenewAsync(CancellationToken cancellationToken)
    {
        return _client.RenewAsync(cancellationToken);
    }

    public Task<SiPassSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        // Placeholder until MS-API integration is implemented.
        return Task.FromResult(new SiPassSnapshot(
            IsConnected: true,
            DataStale: false,
            SnapshotAt: DateTimeOffset.UtcNow,
            Devices: Array.Empty<SiPassDeviceSnapshot>()));
    }
}
