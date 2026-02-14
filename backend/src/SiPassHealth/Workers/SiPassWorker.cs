using Microsoft.EntityFrameworkCore;
using SiPassHealth.SiPass;
using SiPassHealth.StateEngine;
using SiPassHealth.Storage;
using SiPassHealth.Web;

namespace SiPassHealth.Workers;

public sealed class SiPassWorker : BackgroundService
{
    private readonly ILogger<SiPassWorker> _logger;
    private readonly ISiPassProvider _provider;
    private readonly SipassConnectionState _connection;
    private readonly StateEngine.StateEngine _stateEngine;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly SseBroker _sse;
    private readonly StateEngineOptions _options;
    private readonly SipassOptions _sipassOptions;
    private bool? _lastConnected;

    public SiPassWorker(
        ILogger<SiPassWorker> logger,
        ISiPassProvider provider,
        SipassConnectionState connection,
        StateEngine.StateEngine stateEngine,
        IDbContextFactory<AppDbContext> dbFactory,
        SseBroker sse,
        IOptions<StateEngineOptions> options,
        IOptions<SipassOptions> sipassOptions)
    {
        _logger = logger;
        _provider = provider;
        _connection = connection;
        _stateEngine = stateEngine;
        _dbFactory = dbFactory;
        _sse = sse;
        _options = options.Value;
        _sipassOptions = sipassOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickInterval = TimeSpan.FromSeconds(_options.TickSeconds);
        var renewInterval = TimeSpan.FromSeconds(_sipassOptions.RenewIntervalSeconds);
        var nextRenew = DateTimeOffset.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            var allowTick = true;
            try
            {
                if (DateTimeOffset.UtcNow >= nextRenew)
                {
                    await _provider.RenewAsync(stoppingToken);
                    nextRenew = DateTimeOffset.UtcNow.Add(renewInterval);
                }

                var snapshot = await _provider.GetSnapshotAsync(stoppingToken);
                allowTick = !snapshot.DataStale;
                _connection.UpdateStatus(snapshot.IsConnected, snapshot.DataStale, snapshot.SnapshotAt);
                PublishConnectionChange(snapshot.IsConnected);

                if (!snapshot.DataStale)
                {
                    foreach (var device in snapshot.Devices)
                    {
                        _stateEngine.UpdateRawStatus(device.ObjectId, device.Type, device.IsOnline, device.SeenAt);
                    }

                    await PersistObjectsAsync(snapshot.Devices, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SiPass connection issue");
                _connection.MarkFailure();
                PublishConnectionChange(false);
                allowTick = false;
            }

            if (allowTick)
            {
                var changes = _stateEngine.Tick(DateTimeOffset.UtcNow).ToList();
                if (changes.Count > 0)
                {
                    foreach (var change in changes)
                    {
                        _sse.Publish("statusChanged", change);
                    }
                }
            }

            await PersistSnapshotsAsync(stoppingToken);
            await Task.Delay(tickInterval, stoppingToken);
        }
    }

    private async Task PersistSnapshotsAsync(CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var snapshot = _stateEngine.Snapshot();
        foreach (var item in snapshot)
        {
            var existing = await db.StatusSnapshots.FindAsync(new object?[] { item.ObjectId }, cancellationToken);
            if (existing is null)
            {
                existing = new StatusSnapshot { ObjectId = item.ObjectId };
                db.StatusSnapshots.Add(existing);
            }

            existing.RawComms = item.RawOnline ?? false;
            existing.DerivedState = item.DerivedState;
            existing.OfflineSince = item.OfflineSince;
            existing.LastOnlineAt = item.LastOnlineAt;
            existing.LastSeenAt = item.LastSeenAt;
            existing.FlapCount24h = item.FlapCount24h;
            existing.TotalOfflineSeconds24h = item.TotalOfflineSeconds24h;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task PersistObjectsAsync(IEnumerable<SiPassDeviceSnapshot> devices, CancellationToken cancellationToken)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        foreach (var device in devices)
        {
            var existing = await db.Objects.FindAsync(new object?[] { device.ObjectId }, cancellationToken);
            if (existing is null)
            {
                existing = new ObjectEntity { ObjectId = device.ObjectId };
                db.Objects.Add(existing);
            }

            existing.Type = device.Type;
            existing.Name = device.Name;
            existing.Path = device.Path;
            existing.ParentId = device.ParentId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private void PublishConnectionChange(bool connected)
    {
        if (_lastConnected.HasValue && _lastConnected.Value == connected)
        {
            return;
        }

        _lastConnected = connected;
        _sse.Publish("sipassConnectionChanged", new { connected });
    }
}
